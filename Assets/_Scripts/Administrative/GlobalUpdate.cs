// GlobalUpdate just provides an Update() call to anyone who wants it, including non-Monobehaviour classes
public class GlobalUpdate : Singleton<GlobalUpdate> {
    public delegate void GlobalUpdateEvent();
    public event GlobalUpdateEvent UpdateGlobal;
    public event GlobalUpdateEvent LateUpdateGlobal;
    public event GlobalUpdateEvent FixedUpdateGlobal;
    
    void Update() => UpdateGlobal?.Invoke();

    private void LateUpdate() => LateUpdateGlobal?.Invoke();

    private void FixedUpdate() => FixedUpdateGlobal?.Invoke();
}
