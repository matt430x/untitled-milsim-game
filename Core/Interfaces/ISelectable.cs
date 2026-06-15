namespace MilSim.Core.Interfaces;

public interface ISelectable
{
    int OwnerId { get; }
    bool IsSelected { get; }
    void Select();
    void Deselect();
}
