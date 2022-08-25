namespace Nova.Compat
{
    
    internal class NotKeyableAttribute
#if ANIMATIONS
     : UnityEngine.Animations.NotKeyableAttribute
#else
     : System.Attribute
#endif
    {

    }
}
