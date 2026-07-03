using Godot;
using System;
using EternalForest.Game;

public partial class Player : CharacterBody2D
{
	[Export] private int _speed = 300;
	[Export] private AnimatedSprite2D _sprite;
	[Export] private Camera2D _camera; // Référence à la caméra
	
	[Export] private string _leftAxis = "left";
	[Export] private string _rightAxis = "right";
	[Export] private string _upAxis = "up";
	[Export] private string _downAxis = "down";

	private Vector2 _movementInput = Vector2.Zero;
	private string _lastDirection = "R";
	
	private int _damage = 20; // Dégâts par défaut (maintenant remplacés par GetCurrentDamage())
	public int _life = 20; // vie du joueur
	
	private bool _isDead = false; // état du joueur
	
	private float _attackCooldown = 0f;
	private const float AttackCooldownDuration = 0.8f;
	
	private bool _isAttacking = false; // état d'attaque du joueur

	// Variables pour le boost de vitesse
	private float _speedBoostTimer = 0f;
	private bool _hasSpeedBoost = false;
	private int _baseSpeed = 300;

	// Référence au Sprite de l'objet tenu en main
	private Sprite2D _heldItemSprite;

	public override void _EnterTree()
	{
		if (GetNode<GameManager>("/root/GameManager").IsNewGame)
		{
			GlobalPosition = new Vector2(1500, 1500); // Position initiale du joueur
		}
		else
		{
			int id = int.Parse(GetName());
			SetMultiplayerAuthority(id);
			GlobalPosition = new Vector2(1500, 1500); // Position initiale du joueur
		}
	}
	
	public override void _Ready()
	{
		_baseSpeed = _speed; // Conserver la vitesse de départ

		// Créer et attacher le sprite pour l'objet tenu en main
		_heldItemSprite = new Sprite2D();
		_heldItemSprite.Name = "HeldItem";
		_heldItemSprite.Scale = new Vector2(0.24f, 0.24f);
		_heldItemSprite.Centered = true;
		_heldItemSprite.YSortEnabled = true;
		AddChild(_heldItemSprite);

		// Si ce n'est pas le joueur local, on désactive le traitement des entrées
		if (!IsMultiplayerAuthority())
		{
			SetProcess(false);
			SetPhysicsProcess(false);
			if (_camera != null)
			{
				_camera.Enabled = false; // Désactive la caméra pour les autres joueurs
			}
			return;
		}
		if (IsMultiplayerAuthority())
		{
			_camera.Enabled = true;
			_camera.MakeCurrent(); // Fait de cette caméra la caméra active
		}
		
		// Ajout du joueur au groupe "Players"
		AddToGroup("Players");
	}

	public override void _Process(double delta)
	{
		if (!IsMultiplayerAuthority() || _isDead) return;
		
		// Gérer le cooldown de vitesse
		if (_hasSpeedBoost)
		{
			_speedBoostTimer -= (float)delta;
			if (_speedBoostTimer <= 0f)
			{
				_hasSpeedBoost = false;
				_speed = _baseSpeed;
				GD.Print("Vitesse retournée à la normale.");
			}
		}

		if (_attackCooldown > 0f)
		{
			_attackCooldown -= (float)delta;
			if (_attackCooldown <= 0f)
			{
				_isAttacking = false;
			}
		}
		
		_movementInput = new Vector2(
			Input.GetAxis(_leftAxis, _rightAxis),
			Input.GetAxis(_upAxis, _downAxis)
		).Normalized();

		Velocity = _movementInput * _speed;
		MoveAndSlide();

		if (Input.IsActionPressed("attack") || _isAttacking)
		{
			PerformAttack();
		}
		else
		{
			UpdateAnimation();
		}
		
		CheckLife();
		UpdateHeldItemVisual();
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsMultiplayerAuthority() || _isDead) return;

		// Utiliser un consommable de la hotbar avec un clic droit
		if (@event is InputEventMouseButton mouseClick && mouseClick.Pressed && mouseClick.ButtonIndex == MouseButton.Right)
		{
			// Utiliser uniquement si l'inventaire principal est fermé
			if (InventoryGui.Instance != null && !InventoryGui.Instance.Visible)
			{
				TryUseSelectedHotbarItem();
			}
		}
	}

	private void TryUseSelectedHotbarItem()
	{
		if (InventoryGui.Instance == null) return;

		int selectedSlot = InventoryGui.Instance.SelectedHotbarSlot;
		Item item = InventoryGui.Instance.GetHotbarItem(selectedSlot);
		if (item == null) return;

		bool consumed = false;

		if (item.Name == "pomme_rouge")
		{
			if (_life < 20)
			{
				_life = Math.Min(20, _life + 4);
				HeartHealth.Instance?.UpdateHearts(_life, 20);
				consumed = true;
				GD.Print("Pomme rouge consommée. Vie +4 (Actuelle: ", _life, "/20)");
			}
			else
			{
				GD.Print("Vie déjà au maximum !");
			}
		}
		else if (item.Name == "viande_cuite")
		{
			if (_life < 20)
			{
				_life = Math.Min(20, _life + 8);
				HeartHealth.Instance?.UpdateHearts(_life, 20);
				consumed = true;
				GD.Print("Viande cuite consommée. Vie +8 (Actuelle: ", _life, "/20)");
			}
			else
			{
				GD.Print("Vie déjà au maximum !");
			}
		}
		else if (item.Name == "potion_vie")
		{
			if (_life < 20)
			{
				_life = Math.Min(20, _life + 15);
				HeartHealth.Instance?.UpdateHearts(_life, 20);
				consumed = true;
				GD.Print("Potion de vie consommée. Vie +15 (Actuelle: ", _life, "/20)");
			}
			else
			{
				GD.Print("Vie déjà au maximum !");
			}
		}
		else if (item.Name == "potion_vitesse")
		{
			_speed = (int)(_baseSpeed * 1.6f); // +60% de vitesse
			_speedBoostTimer = 8f; // Dure 8 secondes
			_hasSpeedBoost = true;
			consumed = true;
			GD.Print("Potion de vitesse consommée. Vitesse boostée !");
		}

		if (consumed)
		{
			item.Quantity--;
			if (item.Quantity <= 0)
			{
				InventoryGui.Instance.SetHotbarItem(selectedSlot, null);
			}
			else
			{
				InventoryGui.Instance.SetHotbarItem(selectedSlot, item);
			}
		}
	}

	private int GetCurrentDamage()
	{
		if (InventoryGui.Instance == null) return 5;

		Item equipped = InventoryGui.Instance.GetSelectedHotbarItem();
		if (equipped == null) return 5; // Dégâts à mains nues (5 PV)

		switch (equipped.Name)
		{
			case "epee_bois": return 8;
			case "epee_pierre": return 14;
			case "epee_fer": return 25;
			case "hache_bois": return 6;
			case "hache_pierre": return 10;
			case "hache_fer": return 18;
			case "pioche_bois": return 6;
			case "pioche_pierre": return 10;
			case "pioche_fer": return 18;
			default: return 5;
		}
	}

	private void UpdateHeldItemVisual()
	{
		if (_heldItemSprite == null) return;

		if (InventoryGui.Instance == null)
		{
			_heldItemSprite.Texture = null;
			return;
		}

		Item equipped = InventoryGui.Instance.GetSelectedHotbarItem();
		// On affiche l'objet uniquement si c'est une arme, un outil, ou une ressource
		if (equipped == null || string.IsNullOrEmpty(equipped.Name))
		{
			_heldItemSprite.Texture = null;
			return;
		}

		_heldItemSprite.Texture = equipped.Icon as Texture2D;

		// Si on n'est pas en train d'attaquer, on positionne l'objet tenu normalement
		if (!_isAttacking)
		{
			if (_lastDirection == "R")
			{
				_heldItemSprite.Position = new Vector2(10, -22);
				_heldItemSprite.RotationDegrees = 35;
				_heldItemSprite.Scale = new Vector2(0.24f, 0.24f);
			}
			else
			{
				_heldItemSprite.Position = new Vector2(-10, -22);
				_heldItemSprite.RotationDegrees = -35;
				_heldItemSprite.Scale = new Vector2(-0.24f, 0.24f); // Flip horizontal
			}
		}
	}

	private void AnimateWeaponSwing()
	{
		if (_heldItemSprite == null || _heldItemSprite.Texture == null) return;

		Tween tween = CreateTween();
		float startRot = _lastDirection == "R" ? -45f : 45f;
		float targetRot = _lastDirection == "R" ? 90f : -90f;
		float endRot = _lastDirection == "R" ? 35f : -35f;

		// Animation de swing : rotation rapide vers l'avant, puis retour au repos
		_heldItemSprite.RotationDegrees = startRot;
		tween.TweenProperty(_heldItemSprite, "rotation_degrees", targetRot, AttackCooldownDuration * 0.4f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(_heldItemSprite, "rotation_degrees", endRot, AttackCooldownDuration * 0.4f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
	}

	private void PerformAttack()
	{
		if (_isAttacking && _attackCooldown > 0f) return;

		_isAttacking = true;
		_attackCooldown = AttackCooldownDuration;

		bool isSolo = GetNode<GameManager>("/root/GameManager").IsNewGame;

		string animation = _lastDirection == "R" ? 
			(isSolo ? "attack_right" : "attack2_right") : 
			(isSolo ? "attack_left" : "attack2_left");

		PlayAnimationSync(animation);
		AnimateWeaponSwing(); // Déclencher l'animation visuelle de balancement de l'outil tenu en main !

		if (isSolo)
		{
			AttackNearbyOrcs();
			
			// Si le joueur tient une pioche, il peut miner les rochers/minerais proches
			Item equipped = InventoryGui.Instance?.GetSelectedHotbarItem();
			if (equipped != null && equipped.Name.Contains("pioche"))
			{
				HarvestNearbyRocks();
			}
		}
		else
		{
			AttackNearbyPlayers();
		}
	}
	
	private void AttackNearbyPlayers()
	{
		int damage = GetCurrentDamage() * 2; // PvP double les dégâts
		
		float attackRange = 45f;
		Vector2 attackDirection = _lastDirection == "R" ? Vector2.Right : Vector2.Left;
		Vector2 attackOrigin = GlobalPosition + attackDirection * 20f;

		foreach (Node node in GetTree().GetNodesInGroup("Players"))
		{
			if (node is Player otherPlayer && otherPlayer != this)
			{
				float distance = otherPlayer.GlobalPosition.DistanceTo(attackOrigin);
				if (distance <= attackRange)
				{
					// Utiliser un RPC ciblé
					int targetPeerId = otherPlayer.GetMultiplayerAuthority();
					if (targetPeerId != 0)
					{
						RpcId(targetPeerId, nameof(TakeDamage), damage);
					}
				}
			}
		}
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority)]
	public void TakeDamage(int damage)
	{
		_life -= damage;
		CheckLife();

		if (IsMultiplayerAuthority())
		{
			HeartHealth.Instance?.UpdateHearts(_life, 20);
		}
	}

	private void AttackNearbyOrcs()
	{
		int damage = GetCurrentDamage();
		float attackRange = 45f; // Distance maximale pour toucher un orc
		Vector2 attackDirection = _lastDirection == "R" ? Vector2.Right : Vector2.Left;
		Vector2 attackOrigin = GlobalPosition + attackDirection * 20f;

		foreach (Node node in GetTree().GetNodesInGroup("Orcs"))
		{
			if (node is Orc orc)
			{
				float distance = orc.GlobalPosition.DistanceTo(attackOrigin);
				if (distance <= attackRange)
				{
					string dir = _lastDirection == "R" ? "right" : "left";
					orc.TakeDamage(damage, dir);
				}
			}
			else if (node is God god)
			{
				float distance = god.GlobalPosition.DistanceTo(attackOrigin);
				if (distance <= attackRange)
				{
					string dir = _lastDirection == "R" ? "right" : "left";
					god.TakeDamage(damage, dir);
				}
			}
		}
	}

	private void HarvestNearbyRocks()
	{
		float harvestRange = 45f; // Distance max de piochage
		Vector2 attackDirection = _lastDirection == "R" ? Vector2.Right : Vector2.Left;
		Vector2 attackOrigin = GlobalPosition + attackDirection * 20f;

		foreach (Node node in GetTree().GetNodesInGroup("Rocks"))
		{
			if (node is Node2D rock)
			{
				float distance = rock.GlobalPosition.DistanceTo(attackOrigin);
				if (distance <= harvestRange)
				{
					// Récupérer la vie actuelle du rocher
					int health = 3;
					if (rock.HasMeta("health"))
					{
						health = (int)rock.GetMeta("health");
					}
					health--;

					GD.Print($"Rocher miné ! Vie restante: {health}");

					// Animation de secousse (effet d'impact de pioche)
					Node2D sprite = rock.GetNodeOrNull<Node2D>("Sprite2D");
					if (sprite != null)
					{
						Vector2 baseScale = sprite.Scale;
						Tween tween = rock.CreateTween();
						tween.TweenProperty(sprite, "scale", baseScale * 0.8f, 0.05f);
						tween.TweenProperty(sprite, "scale", baseScale, 0.05f);
					}

					if (health <= 0)
					{
						BreakRock(rock);
					}
					else
					{
						rock.SetMeta("health", health);
					}
					break; // On ne mine qu'un rocher à la fois par coup
				}
			}
		}
	}

	private void BreakRock(Node2D rock)
	{
		GD.Print("Rocher cassé !");
		Vector2 spawnPos = rock.GlobalPosition;
		
		// Déterminer le type d'objet qui tombe
		string nameLower = rock.Name.ToString().ToLower();
		string itemName = "pierre";
		string texturePath = "res://Inven/pierre.png";
		int quantity = (int)GD.RandRange(1, 3);
		int maxStack = 64;

		if (nameLower.Contains("iron") || nameLower.Contains("fer"))
		{
			itemName = "minerai_fer";
			texturePath = "res://Inven/minerai_fer.png";
			quantity = (int)GD.RandRange(1, 2);
		}

		// Créer et faire tomber les blocs sous forme de CollectibleItem
		for (int i = 0; i < quantity; i++)
		{
			CollectibleItem drop = new CollectibleItem();
			drop.Position = spawnPos;
			drop.Setup(itemName, texturePath, 1, maxStack);
			
			// L'ajouter à la scène pour affichage et ramassage
			rock.GetParent()?.AddChild(drop);
		}

		rock.QueueFree(); // Supprime le rocher de la carte
	}

	private void UpdateAnimation()
	{
		if (_movementInput.X > 0)
		{
			_lastDirection = "R";
			PlayAnimationSync("walk_right");
		}
		else if (_movementInput.X < 0)
		{
			_lastDirection = "L";
			PlayAnimationSync("walk_left");
		}
		else if (_movementInput.Y != 0)
		{
			PlayAnimationSync(_lastDirection == "R" ? "walk_right" : "walk_left");
		}
		else
		{
			PlayAnimationSync("");
		}
	}
	
	private void CheckLife()
	{
		if (_life <= 0 && !_isDead)
		{
			_isDead = true;
			Die();
		}
	}

	private void Die()
	{
		SetProcess(false);
		SetPhysicsProcess(false);
		
		string animation = _lastDirection == "R" ? "death_right" : "death_left";
		
		PlayAnimationSync(animation);
		
		Timer deathtimer = new Timer();
		deathtimer.WaitTime = 1f;
		deathtimer.OneShot = true;
		deathtimer.Connect("timeout", Callable.From(() => FinishDeath()));
		AddChild(deathtimer);
		deathtimer.Start();
	}
	
	private void FinishDeath()
	{
		QueueFree();
		GetTree().ChangeSceneToFile("res://UI/GameOver.tscn");
	}
	
	public void PlayAnimationSync(string animation)
	{
		if (animation == "")
		{
			_sprite.Stop();
		}
		else
		{
			_sprite.Play(animation);
		}
		
		if (IsMultiplayerAuthority())
		{
			Rpc("RemotePlayAnimation", animation);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void RemotePlayAnimation(string animation)
	{
		if (IsMultiplayerAuthority()) return;
		if (animation == "")
		{
			_sprite.Stop();
		}
		else
		{
			_sprite.Play(animation);
		}
	}
}
