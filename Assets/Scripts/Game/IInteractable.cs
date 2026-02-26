namespace Game
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        void InteractTapped();
        void InteractHeld();
        void InteractDown();
        void InteractUp();
        void InteractCancel();
    }
}