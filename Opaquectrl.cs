using System;
using System.Reflection;
using System.Windows;
using HarmonyLib;
using Microsoft;

namespace com.iavjkae.opaquectrl
{
    internal class Opaquectrl
    {
        private readonly Harmony _harmony;
        private bool _hooked;
        private static readonly string HookedTypeName = "System.Windows.Media.Visual";
        private static readonly string CompletionControlTypeName = "Microsoft.VisualStudio.Language.Intellisense.Implementation.DefaultCompletionSessionPresenter";
        private static readonly string QuickDetectionTypeName = "DefaultCompletionSessionPresenter";
        private static readonly Type _hookedType;
        private static readonly MethodInfo _target;
        private static readonly MethodInfo _prefixHook;

        static Opaquectrl()
        {
            const string hookedNameFilter = "PresentationCore";
            foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_hookedType == null && asm.FullName.StartsWith(hookedNameFilter))
                {
                    _hookedType = asm.GetType(HookedTypeName);
                    break;
                }
            }

            Requires.NotNull(_hookedType, nameof(_hookedType));
            const string propertyName = "VisualOpacity";
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            _target = _hookedType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance).SetMethod;
            _prefixHook = typeof(Opaquectrl).GetMethod(nameof(PrefixHookSetVisualOpacity),BindingFlags.NonPublic | BindingFlags.Static);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            Requires.NotNull(_target, nameof(_target));
            Requires.NotNull(_prefixHook, nameof(_prefixHook));
        }

        public Opaquectrl()
        {
            _harmony = new Harmony(typeof(Opaquectrl).Namespace);
        }

        private static bool PrefixHookSetVisualOpacity(UIElement __instance,double value)
        {
            // HACK: 0.3 is a hack value
            if(value != 0.3)
            {
                return true;
            }
            // prevent completion dialog transparent
            var type = __instance.GetType();
            if (type.FullName.Length == CompletionControlTypeName.Length && 
                type.FullName.EndsWith(QuickDetectionTypeName))
            { 
                return false; 
            }
            return true;
        }

        public void Start()
        {
            if(_hooked)
            {
                return;
            }
            _harmony.Patch(_target,new HarmonyMethod(_prefixHook));
            _hooked = true;
        }

        public bool IsWorking()
        {
            return _hooked;
        }

        public void Stop()
        {
            if( _hooked)
            {
                _harmony.Unpatch(_target, HarmonyPatchType.Prefix,typeof(Opaquectrl).Namespace);
                _hooked = false;
            }
        }
    }
}
