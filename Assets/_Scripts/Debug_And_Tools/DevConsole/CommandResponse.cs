namespace DeveloperConsole {
    public abstract class CommandResponse {
        public abstract bool WasSuccessful { get; }
        
        public static implicit operator bool(CommandResponse response) => response.WasSuccessful;
    }
    
    public class SuccessResponse : CommandResponse {
        public override bool WasSuccessful => true;
    }
    
    public class FailureResponse : CommandResponse {
        public override bool WasSuccessful => false;
        public string Reason { get; }
        
        public FailureResponse(string reason) {
            Reason = reason;
        }
    }
}
