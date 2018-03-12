namespace ConversationLibrary.Utility
{
    using System;
    using System.Collections.Generic;

    public static class CheapContainer
    {
        public static void Register<I,C>() 
            where C : class, I, new()
            where I : class
        {
            object instance = null;
            C typedInstance = null;

            if (instanceMap.TryGetValue(typeof(C), out instance))
            {
                typedInstance = (C)instance;
            }
            else
            {
                typedInstance = new C();
                instanceMap[typeof(C)] = typedInstance;
            }
            typeMap[typeof(I)] = typedInstance;
        }
        public static I Resolve<I>() where I : class 
        {
            return (typeMap[typeof(I)] as I);
        }
        static Dictionary<Type, object> instanceMap = new Dictionary<Type, object>();
        static Dictionary<Type, object> typeMap = new Dictionary<Type, object>();
    }
}