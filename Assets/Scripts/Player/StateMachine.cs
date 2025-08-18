public interface IState
{
    void Enter();
    void Tick(float dt);
    void Exit();
}

public class StateMachine
{
    public IState Current { get; private set; }

    public void ChangeState(IState next)
    {
        if (next == null) return;
        if (Current == next) return;

        Current?.Exit();
        Current = next;
        Current?.Enter();
    }

    public void Tick(float dt) => Current?.Tick(dt);
}
