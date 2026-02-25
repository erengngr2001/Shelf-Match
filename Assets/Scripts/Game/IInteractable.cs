namespace Game
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        void InteractTapped();
        void InteractHeld();
    }
}