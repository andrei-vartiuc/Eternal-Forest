using Godot;
using System;

public partial class CollectibleItem : Area2D
{
    private string _itemName;
    private string _texturePath;
    private int _quantity;
    private int _maxStack;
    
    private Sprite2D _sprite;
    private bool _isCollecting = false;
    private Node2D _targetPlayer = null;

    public void Setup(string name, string texturePath, int quantity, int maxStack)
    {
        _itemName = name;
        _texturePath = texturePath;
        _quantity = quantity;
        _maxStack = maxStack;
    }

    public override void _Ready()
    {
        // Configurer la détection de collision
        Monitoring = true;
        CollisionMask = 1; // Collision avec le joueur (Layer 1)
        CollisionLayer = 0; // Pas besoin d'être détecté par d'autres
        
        BodyEntered += OnBodyEntered;

        // Créer le Sprite2D s'il n'existe pas
        _sprite = new Sprite2D();
        if (!string.IsNullOrEmpty(_texturePath))
        {
            _sprite.Texture = ResourceLoader.Load<Texture2D>(_texturePath);
        }
        _sprite.Scale = new Vector2(0.8f, 0.8f); // Taille équilibrée sur le sol
        AddChild(_sprite);

        // Ajouter un CollisionShape2D pour la détection
        CollisionShape2D colShape = new CollisionShape2D();
        colShape.Shape = new CircleShape2D { Radius = 30f };
        AddChild(colShape);

        // Animation d'apparition (petit saut)
        var tween = CreateTween();
        Vector2 startPos = Position;
        Vector2 peakPos = startPos + new Vector2((float)GD.RandRange(-20, 20), -30);
        Vector2 landPos = startPos + new Vector2((float)GD.RandRange(-40, 40), 10);
        
        tween.TweenProperty(this, "position", peakPos, 0.2f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "position", landPos, 0.25f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    }

    public override void _Process(double delta)
    {
        if (_isCollecting && _targetPlayer != null)
        {
            // Se déplacer vers le joueur (effet d'aspiration magnétique !)
            Position = Position.Lerp(_targetPlayer.Position, 15f * (float)delta);
            _sprite.Scale = _sprite.Scale.Lerp(Vector2.Zero, 10f * (float)delta);
            
            if (Position.DistanceTo(_targetPlayer.Position) < 15f)
            {
                Collect();
            }
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body is Player player && !_isCollecting)
        {
            _isCollecting = true;
            _targetPlayer = player;
        }
    }

    private void Collect()
    {
        if (InventoryGui.Instance != null)
        {
            Item item = new Item(_itemName, _sprite.Texture, _quantity, _maxStack);
            if (InventoryGui.Instance.AddItem(item))
            {
                GD.Print($"Objet ramassé : {_itemName} (x{_quantity})");
                QueueFree();
            }
            else
            {
                // Inventaire plein, on annule l'aspiration et on remet au sol
                _isCollecting = false;
                _targetPlayer = null;
                _sprite.Scale = new Vector2(0.8f, 0.8f);
            }
        }
    }
}
