using System;

namespace ConduitNet.Core
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ConduitFilterAttribute : Attribute
    {
        public Type FilterType { get; }

        public ConduitFilterAttribute(Type filterType)
        {
            if (!typeof(IConduitFilter).IsAssignableFrom(filterType))
            {
                throw new ArgumentException($"Type {filterType.Name} must implement IConduitFilter", nameof(filterType));
            }
            FilterType = filterType;
        }
    }
}
