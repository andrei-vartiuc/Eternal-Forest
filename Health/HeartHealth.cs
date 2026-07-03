using Godot;
using System;

public partial class HeartHealth : HBoxContainer
{
    public static HeartHealth Instance { get; private set; }

    private PackedScene HeartGui;
    private Texture2D _fullHeartTex;
    private Texture2D _halfHeartTex;
    private Texture2D _emptyHeartTex;

    public override void _Ready()
    {
        Instance = this;

        HeartGui = ResourceLoader.Load<PackedScene>("res://Health/panel.tscn");
        _fullHeartTex = ResourceLoader.Load<Texture2D>("res://Health/coeurMinecraft.png");
        _halfHeartTex = ResourceLoader.Load<Texture2D>("res://Health/halfcoeur.png");
        _emptyHeartTex = ResourceLoader.Load<Texture2D>("res://Health/coeurvide.png");

        // Initialiser avec 10 cœurs pleins (pour 20 HP max)
        UpdateHearts(20, 20);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        // Limiter currentHealth entre 0 et maxHealth
        currentHealth = Math.Clamp(currentHealth, 0, maxHealth);

        int totalHearts = maxHealth / 2;
        if (totalHearts <= 0) totalHearts = 10;

        // Ajuster le nombre d'enfants (panels de cœur)
        int currentChildCount = GetChildCount();
        if (currentChildCount < totalHearts)
        {
            for (int i = currentChildCount; i < totalHearts; i++)
            {
                var heart = HeartGui.Instantiate();
                AddChild(heart);
            }
        }
        else if (currentChildCount > totalHearts)
        {
            for (int i = currentChildCount - 1; i >= totalHearts; i--)
            {
                Node child = GetChild(i);
                RemoveChild(child);
                child.QueueFree();
            }
        }

        // Mettre à jour la texture de chaque cœur (plein, demi, vide)
        for (int i = 0; i < totalHearts; i++)
        {
            Node panel = GetChild(i);
            Sprite2D sprite = panel.GetNodeOrNull<Sprite2D>("Sprite2D");
            if (sprite != null)
            {
                int heartValue = i * 2;
                if (currentHealth >= heartValue + 2)
                {
                    sprite.Texture = _fullHeartTex;
                    sprite.Position = new Vector2(20, 20); // Parfaitement centré
                }
                else if (currentHealth == heartValue + 1)
                {
                    sprite.Texture = _halfHeartTex;
                    sprite.Position = new Vector2(20, 20); // Parfaitement centré
                }
                else
                {
                    sprite.Texture = _emptyHeartTex;
                    sprite.Position = new Vector2(20, 20); // Parfaitement centré
                }
            }
        }
    }
}
