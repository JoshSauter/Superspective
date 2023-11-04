using UnityEngine;

public abstract class PlayerMovementComponent {
    protected readonly PlayerMovement m;
    protected readonly Transform transform;

    protected PlayerMovementComponent(PlayerMovement movement) {
        this.m = movement;
        this.transform = movement.transform;
    }

    private PlayerMovementComponent() { }

    public abstract void Init();
}
