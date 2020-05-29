using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace zenDzeeMods
{
    internal class ZenDzeeCompatibilityHelper
    {
        private static T InvokeObjectManagerGenericMethod<T>(string methodName, object[] parameters) where T : class
        {
            Game game = Game.Current;
            Type gameType = game.GetType();
            if (gameType == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: gameType is null"));
                return null;
            }
            MethodInfo getObjectManagerMethod = gameType.GetMethod("get_ObjectManager", new Type[] { });
            if (getObjectManagerMethod == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: get_ObjectManager method is null"));
                return null;
            }
            object objectManager = getObjectManagerMethod.Invoke(game, new object[] { });
            if (objectManager == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: objectManager is null"));
                return null;
            }
            Type objectManagerType = objectManager.GetType();
            if (objectManagerType == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: objectManagerType is null"));
                return null;
            }

            MethodInfo method = objectManagerType.GetMethods()
                .FirstOrDefault(m =>
                    m.Name == methodName
                    && m.IsGenericMethod
                    && m.GetGenericArguments().Length == 1
                    && m.GetParameters().Length == parameters.Length);
            if (method == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: " + methodName + " method is null"));
                return null;
            }

            MethodInfo genericMethod = method.MakeGenericMethod(typeof(T));

            return genericMethod.Invoke(objectManager, parameters) as T;
        }

        public static T RegisterPresumedObject<T>(T obj) where T : class
        {
            return InvokeObjectManagerGenericMethod<T>("RegisterPresumedObject", new object[] { obj });
        }

        public static void SetTextVariable(TextObject text, string tag, int variable)
        {
            SetTextVariable_internal<int>(text, tag, variable);
        }

        public static void SetTextVariable(TextObject text, string tag, TextObject variable)
        {
            SetTextVariable_internal<TextObject>(text, tag, variable);
        }

        private static void SetTextVariable_internal<T>(TextObject text, string tag, object variable)
        {
            Type textType = text.GetType();
            if (textType == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: textType is null"));
                return;
            }
            MethodInfo setTextVariableMethod = textType.GetMethod("SetTextVariable", new Type[] { typeof(string), typeof(T) });
            if (setTextVariableMethod == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: setTextVariableMethod is null"));
                return;
            }
            setTextVariableMethod.Invoke(text, new object[] { tag, variable });
        }
    }
}
