namespace Deneme_proje
{
    using System;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AllowAnonymousAttribute : Attribute
    {
    }
}
