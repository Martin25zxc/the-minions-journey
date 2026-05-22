using UnityEngine;

public interface IInteractable
{
    bool CanInteract(GameObject interactor);
    string GetInteractionDescription();
    void Interact(GameObject interactor);
}