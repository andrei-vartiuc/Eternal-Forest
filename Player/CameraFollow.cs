using Godot;

public partial class CameraFollow : Camera2D
{
	[Export] public Node2D ObjectToFollow;

	public override void _Ready()
	{
		if (ObjectToFollow != null && Enabled)
		{
			Position = ObjectToFollow.Position.Round();
		}
	}
	public override void _Process(double delta)
	{
		if (ObjectToFollow != null && Enabled)
		{
			Position = ObjectToFollow.Position.Round();
		}
	}
}
