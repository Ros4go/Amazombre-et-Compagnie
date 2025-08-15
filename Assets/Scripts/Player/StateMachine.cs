using UnityEngine;

public interface IState
{
    void Enter();
    void Exit();
    void Tick();  // logique d'état (ne pas appeler CharacterController.Move ici)
}

public class StateMachine
{
    public IState Current { get; private set; }

    public void ChangeState(IState next)
    {
        if (Current == next) return;
        Current?.Exit();
        Current = next;
        Current?.Enter();
    }

    public void Tick() => Current?.Tick();
}
