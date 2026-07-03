using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using EternalForest.Game;

public partial class InventoryGui : Control
{
	public static InventoryGui Instance { get; private set; }
	
	private bool _isOpen = false;
	private GridContainer _gridContainer;
	private List<Item> _items;
	private Item _draggedItem;
	private int _draggedFromSlot = -1;
	private bool _draggedFromHotbar = false;
	
	// Variables pour la hotbar
	private List<Item> _hotbarItems = new List<Item>(9);
	private int _selectedHotbarSlot = 0;
	public int SelectedHotbarSlot => _selectedHotbarSlot;
	
	// Pour bouger les elts
	private Control _draggedItemVisual;
	private Sprite2D _draggedItemSprite;
	private Label _draggedItemLabel;
	private InventoryHotbar _hotbarUI;
	
	[Export] private int _maxSlots = 63;
	
	public override void _Ready()
	{
		Instance = this;
		Visible = false;
		SetProcessInput(true);
		_gridContainer = GetNode<GridContainer>("NinePatchRect/GridContainer");
		_items = new List<Item>();
		
		// Initialiser la hotbar avec des éléments vides
		for (int i = 0; i < 9; i++) _hotbarItems.Add(null);
		
		// Pour bouger les items le visuel
		_draggedItemVisual = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(_draggedItemVisual);
		
		_draggedItemSprite = new Sprite2D { Scale = new Vector2(1.2f, 1.2f) };
		_draggedItemVisual.AddChild(_draggedItemSprite);
		
		_draggedItemLabel = new Label { Scale = new Vector2(0.9f, 0.9f) };
		_draggedItemLabel.AddThemeColorOverride("font_color", Colors.White);
		_draggedItemLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
		_draggedItemLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		_draggedItemLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		_draggedItemVisual.AddChild(_draggedItemLabel);
		
		// Ajouter des objets de départ uniquement si c'est une nouvelle partie solo
		if (GetNode<GameManager>("/root/GameManager").IsNewGame)
		{
			AddItem(new Item("epee_bois", ResourceLoader.Load<Texture2D>("res://Inven/epee_bois.png"), 1, 1));
			AddItem(new Item("epee_pierre", ResourceLoader.Load<Texture2D>("res://Inven/epee_pierre.png"), 1, 1));
			AddItem(new Item("epee_fer", ResourceLoader.Load<Texture2D>("res://Inven/epee_fer.png"), 1, 1));
			
			AddItem(new Item("hache_bois", ResourceLoader.Load<Texture2D>("res://Inven/hache_bois.png"), 1, 1));
			AddItem(new Item("hache_pierre", ResourceLoader.Load<Texture2D>("res://Inven/hache_pierre.png"), 1, 1));
			AddItem(new Item("hache_fer", ResourceLoader.Load<Texture2D>("res://Inven/hache_fer.png"), 1, 1));
			
			AddItem(new Item("pioche_bois", ResourceLoader.Load<Texture2D>("res://Inven/pioche_bois.png"), 1, 1));
			AddItem(new Item("pioche_pierre", ResourceLoader.Load<Texture2D>("res://Inven/pioche_pierre.png"), 1, 1));
			AddItem(new Item("pioche_fer", ResourceLoader.Load<Texture2D>("res://Inven/pioche_fer.png"), 1, 1));
			
			AddItem(new Item("buche_bois", ResourceLoader.Load<Texture2D>("res://Inven/buche_bois.png"), 10, 64));
			AddItem(new Item("pierre", ResourceLoader.Load<Texture2D>("res://Inven/pierre.png"), 15, 64));
			AddItem(new Item("minerai_fer", ResourceLoader.Load<Texture2D>("res://Inven/minerai_fer.png"), 5, 64));
			
			AddItem(new Item("pomme_rouge", ResourceLoader.Load<Texture2D>("res://Inven/pomme_rouge.png"), 5, 16));
			AddItem(new Item("viande_cuite", ResourceLoader.Load<Texture2D>("res://Inven/viande_cuite.png"), 3, 16));
			AddItem(new Item("potion_vie", ResourceLoader.Load<Texture2D>("res://Inven/potion_vie.png"), 2, 8));
			AddItem(new Item("potion_vitesse", ResourceLoader.Load<Texture2D>("res://Inven/potion_vitesse.png"), 2, 8));
		}
		
		UpdateUI();
	}
	
	public void Open() { _isOpen = true; Visible = true; UpdateUI(); }
	public void Close() { _isOpen = false; Visible = false; }
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("E"))
		{
			if (_isOpen) Close(); else Open();
		}
		
		// Choisir ou aller sur la hotbar touche 1 à 9 (PhysicalKeycode pour compatibilité AZERTY/QWERTY)
		if (@event is InputEventKey key && key.Pressed && key.PhysicalKeycode >= Key.Key1 && key.PhysicalKeycode <= Key.Key9)
			SelectHotbarSlot((int)key.PhysicalKeycode - (int)Key.Key1);
		
		// Pour passer sur la HB
		if (@event is InputEventMouseButton scroll && scroll.Pressed)
		{
			if (scroll.ButtonIndex == MouseButton.WheelUp) SelectHotbarSlot(_selectedHotbarSlot == 0 ? 8 : _selectedHotbarSlot - 1);
			else if (scroll.ButtonIndex == MouseButton.WheelDown) SelectHotbarSlot(_selectedHotbarSlot == 8 ? 0 : _selectedHotbarSlot + 1);
		}
		
		if (_draggedItem != null && @event is InputEventMouseMotion)
			_draggedItemVisual.GlobalPosition = GetViewport().GetMousePosition();

		// Gérer le relâchement global du clic gauche de la souris pour le drag-and-drop
		if (_draggedItem != null && @event is InputEventMouseButton mouseRelease && mouseRelease.ButtonIndex == MouseButton.Left && !mouseRelease.Pressed)
		{
			Vector2 mousePos = mouseRelease.GlobalPosition;
			int hoveredSlot = GetHoveredSlot(mousePos);
			if (hoveredSlot != -1)
			{
				DropItem(hoveredSlot);
			}
			else
			{
				int hoveredHotbarSlot = GetHoveredHotbarSlot(mousePos);
				if (hoveredHotbarSlot != -1)
				{
					DropHotbarItem(hoveredHotbarSlot);
				}
				else
				{
					CancelDrag();
				}
			}
			GetViewport().SetInputAsHandled();
		}
	}
	
	public void SelectHotbarSlot(int slot) { _selectedHotbarSlot = slot; _hotbarUI?.UpdateUI(); }
	public void RegisterHotbarUI(InventoryHotbar hotbarUI) { _hotbarUI = hotbarUI; _hotbarUI?.UpdateUI(); }
	
	public bool AddItem(Item item)
	{
		if (item == null) return false;
		
		// 1. Essayer d'empiler avec des objets existants du même type
		for (int i = 0; i < _items.Count; i++)
		{
			if (_items[i] != null && _items[i].CanStackWith(item))
			{
				_items[i].TryStackWith(item);
				if (item.Quantity <= 0) return true;
			}
		}
		
		// 2. Placer dans le premier slot vide disponible (valeur null)
		for (int i = 0; i < _items.Count; i++)
		{
			if (_items[i] == null)
			{
				_items[i] = item;
				return true;
			}
		}
		
		// 3. Ajouter à la fin si la liste n'a pas atteint sa capacité max
		if (_items.Count < _maxSlots)
		{
			_items.Add(item);
			return true;
		}
		return false;
	}
	
	public Item GetHotbarItem(int slot) => (slot >= 0 && slot < 9) ? _hotbarItems[slot] : null;
	public Item GetSelectedHotbarItem() => GetHotbarItem(_selectedHotbarSlot);
	
	public void SetHotbarItem(int slot, Item item)
	{
		if (slot >= 0 && slot < 9) { _hotbarItems[slot] = item; _hotbarUI?.UpdateUI(); }
	}
	
	public Item TakeHotbarItem(int slot)
	{
		if (slot >= 0 && slot < 9)
		{
			Item item = _hotbarItems[slot];
			_hotbarItems[slot] = null;
			_hotbarUI?.UpdateUI();
			return item;
		}
		return null;
	}
	
	public void UpdateUI()
	{
		if (_gridContainer == null) return;
		
		// Enleve tt enfants existants
		foreach (Node child in _gridContainer.GetChildren())
		{
			_gridContainer.RemoveChild(child);
			child.QueueFree();
		}

		// Ajoute elts dans l'inv
		for (int i = 0; i < _maxSlots; i++)
		{
			Panel panel = new Panel { CustomMinimumSize = new Vector2(34, 34), Name = $"Panel{i}" };
			int slotIndex = i;
			panel.GuiInput += (@event) => OnSlotInput(@event, slotIndex);
			
			// ajoute des slots
			panel.AddChild(new Sprite2D {
				Texture = ResourceLoader.Load<Texture2D>("res://Inven/SlotInv.png"),
				Position = new Vector2(17, 17),
				Scale = new Vector2(2, 2),
				RegionEnabled = true,
				RegionRect = new Rect2(105, 7, 17, 16)
			});
			
			// ajoute l'icon s'il y a un item (centré et réduit à l'échelle 0.24)
			if (i < _items.Count && _items[i] != null)
			{
				panel.AddChild(new Sprite2D {
					Texture = _items[i].Icon, 
					Position = new Vector2(17, 17),
					Scale = new Vector2(0.24f, 0.24f),
					Centered = true
				});
				
				// ajoute label quantité s'il y a + de 1
				if (_items[i].Quantity > 1)
				{
					Vector2 labelPosition = panel.CustomMinimumSize / 2 + new Vector2(8, -2);
					Label label = new Label {
						Text = _items[i].Quantity.ToString(),
						Position = labelPosition,
						Scale = new Vector2(0.9f, 0.9f)
					};
					label.AddThemeColorOverride("font_color", Colors.White);
					label.AddThemeColorOverride("font_shadow_color", Colors.Black);
					label.AddThemeConstantOverride("shadow_offset_x", 1);
					label.AddThemeConstantOverride("shadow_offset_y", 1);
					panel.AddChild(label);
				}
			}
			
			_gridContainer.AddChild(panel);
		}
	}
	
	private void OnSlotInput(InputEvent @event, int slotIndex)
	{
		if (@event is InputEventMouseButton mouse && mouse.Pressed)
		{
			if (mouse.ButtonIndex == MouseButton.Left && slotIndex < _items.Count && _items[slotIndex] != null)
				StartDragging(_items[slotIndex], slotIndex);
			else if (mouse.ButtonIndex == MouseButton.Right && slotIndex < _items.Count && _items[slotIndex]?.Quantity > 1)
				SplitStack(slotIndex);
		}
	}
	
	public void HandleHotbarSlotInput(InputEvent @event, int slotIndex)
	{
		if (@event is InputEventMouseButton mouse && mouse.Pressed)
		{
			if (mouse.ButtonIndex == MouseButton.Left)
			{
				if (_draggedItem != null) DropHotbarItem(slotIndex);
				else if (slotIndex < _hotbarItems.Count && _hotbarItems[slotIndex] != null)
					StartDraggingFromHotbar(_hotbarItems[slotIndex], slotIndex);
			}
			else if (mouse.ButtonIndex == MouseButton.Right && slotIndex < _hotbarItems.Count && _hotbarItems[slotIndex]?.Quantity > 1)
				SplitHotbarStack(slotIndex);
		}
	}
	
	private void StartDragging(Item item, int fromSlot)
	{
		if (_draggedItem != null) return;
		
		_draggedItem = item;
		_draggedFromSlot = fromSlot;
		_draggedFromHotbar = false;
		_items[fromSlot] = null;
		SetDraggedItemVisual(item);
		UpdateUI();
	}
	
	private void StartDraggingFromHotbar(Item item, int fromSlot)
	{
		if (_draggedItem != null) return;
		
		_draggedItem = item;
		_draggedFromSlot = fromSlot;
		_draggedFromHotbar = true;
		_hotbarItems[fromSlot] = null;
		SetDraggedItemVisual(item);
		_hotbarUI?.UpdateUI();
	}
	
	private void SetDraggedItemVisual(Item item)
	{
		_draggedItemSprite.Texture = item.Icon;
		_draggedItemLabel.Text = item.Quantity.ToString();
		_draggedItemLabel.Visible = item.Quantity > 1;
		_draggedItemVisual.Visible = true;
		_draggedItemVisual.GlobalPosition = GetViewport().GetMousePosition();
	}
	
	private void DropItem(int toSlot)
	{
		if (_draggedItem == null) return;
		
		if (toSlot >= _maxSlots || toSlot < 0)
		{
			toSlot = FindBestSlot(_draggedItem);
			if (toSlot == -1)
			{
				CancelDrag();
				return;
			}
		}
		
		// Remplir la liste si nécessaire
		while (_items.Count <= toSlot) _items.Add(null);
		
		if (toSlot < _items.Count && _items[toSlot] != null)
		{
			if (_items[toSlot].CanStackWith(_draggedItem))
			{
				if (!_items[toSlot].TryStackWith(_draggedItem))
				{
					(_items[toSlot], _draggedItem) = (_draggedItem, _items[toSlot]);
					// Modifier l'origine pour correspondre au nouvel objet porté
					SetDraggedItemVisual(_draggedItem);
					UpdateUI();
					return;
				}
				else _draggedItem = null;
			}
			else
			{
				(_items[toSlot], _draggedItem) = (_draggedItem, _items[toSlot]);
				SetDraggedItemVisual(_draggedItem);
				UpdateUI();
				return;
			}
		}
		else
		{
			_items[toSlot] = _draggedItem;
			_draggedItem = null;
		}
		
		_draggedItemVisual.Visible = false;
		UpdateUI();
	}
	
	private void DropHotbarItem(int toSlot)
	{
		if (_draggedItem == null) return;
		
		if (toSlot < 0 || toSlot >= 9)
		{
			if (!AddItem(_draggedItem))
			{
				CancelDrag();
			}
			else
			{
				_draggedItem = null;
				_draggedItemVisual.Visible = false;
			}
			return;
		}
		
		if (_hotbarItems[toSlot] != null)
		{
			if (_hotbarItems[toSlot].CanStackWith(_draggedItem))
			{
				if (!_hotbarItems[toSlot].TryStackWith(_draggedItem))
				{
					(_hotbarItems[toSlot], _draggedItem) = (_draggedItem, _hotbarItems[toSlot]);
					SetDraggedItemVisual(_draggedItem);
					_hotbarUI?.UpdateUI();
					return;
				}
				else _draggedItem = null;
			}
			else
			{
				(_hotbarItems[toSlot], _draggedItem) = (_draggedItem, _hotbarItems[toSlot]);
				SetDraggedItemVisual(_draggedItem);
				_hotbarUI?.UpdateUI();
				return;
			}
		}
		else
		{
			_hotbarItems[toSlot] = _draggedItem;
			_draggedItem = null;
		}
		
		_draggedItemVisual.Visible = false;
		_hotbarUI?.UpdateUI();
	}
	
	private void CancelDrag()
	{
		if (_draggedItem == null) return;
		
		if (_draggedFromHotbar)
		{
			_hotbarItems[_draggedFromSlot] = _draggedItem;
			_hotbarUI?.UpdateUI();
		}
		else
		{
			while (_items.Count <= _draggedFromSlot) _items.Add(null);
			_items[_draggedFromSlot] = _draggedItem;
			UpdateUI();
		}
		
		_draggedItem = null;
		_draggedItemVisual.Visible = false;
	}
	
	private void SplitStack(int slotIndex)
	{
		Item item = _items[slotIndex];
		if (item == null || item.Quantity <= 1) return;
		
		int splitAmount = item.Quantity > 5 ? 1 : item.Quantity / 2;
		Item splitItem = item.SplitStack(splitAmount);
		
		if (splitItem != null)
		{
			if (!AddItem(splitItem))
			{
				item.TryStackWith(splitItem);
			}
			UpdateUI();
		}
	}
	
	private void SplitHotbarStack(int slotIndex)
	{
		Item item = _hotbarItems[slotIndex];
		if (item == null || item.Quantity <= 1) return;
		
		int splitAmount = item.Quantity > 5 ? 1 : item.Quantity / 2;
		Item splitItem = item.SplitStack(splitAmount);
		
		if (splitItem != null)
		{
			int emptySlot = Array.FindIndex(_hotbarItems.ToArray(), i => i == null);
			
			if (emptySlot >= 0) _hotbarItems[emptySlot] = splitItem;
			else if (!AddItem(splitItem)) item.TryStackWith(splitItem);
			
			_hotbarUI?.UpdateUI();
		}
	}

	private int GetHoveredSlot(Vector2 globalMousePos)
	{
		if (_gridContainer == null) return -1;
		for (int i = 0; i < _gridContainer.GetChildCount(); i++)
		{
			if (_gridContainer.GetChild(i) is Control slotControl)
			{
				if (slotControl.GetGlobalRect().HasPoint(globalMousePos))
				{
					return i;
				}
			}
		}
		return -1;
	}

	private int GetHoveredHotbarSlot(Vector2 globalMousePos)
	{
		if (_hotbarUI != null)
		{
			return _hotbarUI.GetHoveredSlotIndex(globalMousePos);
		}
		return -1;
	}
	
	private int FindBestSlot(Item item) => _items.FindIndex(i => i != null && i.CanStackWith(item) == true && i.Quantity < i.MaxStackSize);
	private int FindEmptySlot() => Enumerable.Range(0, _maxSlots).FirstOrDefault(i => i >= _items.Count || _items[i] == null, -1);
	public bool HasDraggedItem() => _draggedItem != null;

	// === SÉRIALISATION POUR LA SAUVEGARDE ===
	public List<Godot.Collections.Dictionary> SerializeInventory()
	{
		var list = new List<Godot.Collections.Dictionary>();
		for (int i = 0; i < _items.Count; i++)
		{
			list.Add(SerializeItem(_items[i]));
		}
		return list;
	}

	public List<Godot.Collections.Dictionary> SerializeHotbar()
	{
		var list = new List<Godot.Collections.Dictionary>();
		for (int i = 0; i < _hotbarItems.Count; i++)
		{
			list.Add(SerializeItem(_hotbarItems[i]));
		}
		return list;
	}

	private Godot.Collections.Dictionary SerializeItem(Item item)
	{
		if (item == null) return null;
		
		var dict = new Godot.Collections.Dictionary();
		dict["name"] = item.Name;
		dict["quantity"] = item.Quantity;
		dict["max_stack"] = item.MaxStackSize;
		dict["item_type"] = item.ItemType;
		dict["icon_path"] = item.Icon?.ResourcePath ?? "";
		return dict;
	}

	public void DeserializeInventory(List<Godot.Collections.Dictionary> invData)
	{
		_items.Clear();
		foreach (var dict in invData)
		{
			_items.Add(DeserializeItem(dict));
		}
		UpdateUI();
	}

	public void DeserializeHotbar(List<Godot.Collections.Dictionary> hbData)
	{
		_hotbarItems.Clear();
		foreach (var dict in hbData)
		{
			_hotbarItems.Add(DeserializeItem(dict));
		}
		_hotbarUI?.UpdateUI();
	}

	private Item DeserializeItem(Godot.Collections.Dictionary dict)
	{
		if (dict == null) return null;
		
		string name = (string)dict["name"];
		int quantity = (int)dict["quantity"];
		int maxStack = (int)dict["max_stack"];
		string itemType = (string)dict["item_type"];
		string iconPath = (string)dict["icon_path"];
		
		Texture2D icon = null;
		if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
		{
			icon = ResourceLoader.Load<Texture2D>(iconPath);
		}
		
		Item item = new Item(name, icon, quantity, maxStack);
		item.ItemType = itemType;
		return item;
	}
}
