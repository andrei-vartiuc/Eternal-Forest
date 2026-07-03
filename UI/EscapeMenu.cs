using Godot;
using System;

public partial class EscapeMenu : Control
{
    private Button _resumeButton;
    private Button _saveButton;
    private Button _mainMenuButton;
    private Button _quitButton;
    private Label _statusLabel;
    private Tween _statusTween;

    public override void _Ready()
    {
        // Le menu doit traiter les entrées même si le jeu est en pause
        ProcessMode = ProcessModeEnum.Always;

        _resumeButton = GetNode<Button>("CenterContainer/VBoxContainer/Resume");
        _saveButton = GetNode<Button>("CenterContainer/VBoxContainer/Save");
        _mainMenuButton = GetNode<Button>("CenterContainer/VBoxContainer/MainMenu");
        _quitButton = GetNode<Button>("CenterContainer/VBoxContainer/Quit");
        _statusLabel = GetNode<Label>("CenterContainer/VBoxContainer/Status");

        _resumeButton.Pressed += OnResumePressed;
        _saveButton.Pressed += OnSavePressed;
        _mainMenuButton.Pressed += OnMainMenuPressed;
        _quitButton.Pressed += OnQuitPressed;

        // Caché par défaut au démarrage
        Visible = false;
        _statusLabel.Text = "";
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel") || (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape))
        {
            if (Visible)
            {
                ResumeGame();
            }
            else
            {
                OpenMenu();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    private void OpenMenu()
    {
        Visible = true;
        _statusLabel.Text = "";
        _statusLabel.Modulate = new Color(1, 1, 1, 0);

        // Si c'est une partie Solo, on met le jeu en pause
        if (GetTree().CurrentScene != null && GetTree().CurrentScene.Name == "GameSolo")
        {
            GetTree().Paused = true;
        }
    }

    private void ResumeGame()
    {
        Visible = false;
        GetTree().Paused = false;
    }

    private void OnResumePressed()
    {
        ResumeGame();
    }

    private void OnSavePressed()
    {
        // Trouver le nœud AutoSaveSystem sous GameSolo
        Node gameSolo = GetTree().CurrentScene;
        if (gameSolo != null && gameSolo.Name == "GameSolo")
        {
            AutoSaveSystem autoSave = gameSolo.GetNodeOrNull<AutoSaveSystem>("AutoSaveSystem");
            if (autoSave != null)
            {
                autoSave.SaveGameManual();
                ShowStatusMessage("Partie sauvegardée !");
            }
            else
            {
                ShowStatusMessage("Erreur: Impossible de sauvegarder");
            }
        }
        else
        {
            ShowStatusMessage("Sauvegarde indisponible en multijoueur");
        }
    }

    private void ShowStatusMessage(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.Modulate = new Color(1, 1, 1, 1);

        if (_statusTween != null && _statusTween.IsValid())
        {
            _statusTween.Kill();
        }

        _statusTween = CreateTween();
        // Attendre 1.5 seconde puis faire un fade out de 1 seconde
        _statusTween.TweenInterval(1.5f);
        _statusTween.TweenProperty(_statusLabel, "modulate", new Color(1, 1, 1, 0), 1.0f);
    }

    private void OnMainMenuPressed()
    {
        // S'assurer de quitter la pause avant de changer de scène
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://UI/main_menu.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
