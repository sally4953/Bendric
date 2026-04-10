using Bendric.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bendric
{
    public class ScriptRunner
    {
        private Assembly _assembly;
        private string _workingDirectory;

        public ScriptRunner(Assembly assembly, string workingDirectory)
        {
            _assembly = assembly;
            _workingDirectory = workingDirectory;
        }

        public bool Run(RunVerb verb)
        {
            try
            {
                // Find the entry point (Run method)
                var entryType = FindEntryType();
                if (entryType == null)
                {
                    Logger.Error("Could not find entry point in build script");
                    return false;
                }

                // Create instance or invoke static method
                MethodInfo runMethod = entryType.GetMethod("Run", new[] { typeof(RunVerb) });
                if (runMethod == null)
                {
                    Logger.Error("Could not find 'Run(RunVerb)' method in build script");
                    return false;
                }

                object instance = null;
                if (!runMethod.IsStatic)
                {
                    instance = Activator.CreateInstance(entryType);
                }

                Logger.Info($"Executing build script with verb: {verb}");
                runMethod.Invoke(instance, new object[] { verb });

                return true;
            }
            catch (TargetInvocationException ex)
            {
                Logger.Error($"Script execution failed: {ex.InnerException?.Message ?? ex.Message}");
                if (ex.InnerException != null)
                {
                    Logger.Error(ex.InnerException.StackTrace);
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Script execution failed: {ex.Message}");
                Logger.Error(ex.StackTrace);
                return false;
            }
        }

        private Type FindEntryType()
        {
            // Look for a type with a Run method that takes RunVerb
            foreach (Type type in _assembly.GetTypes())
            {
                var runMethod = type.GetMethod("Run", new[] { typeof(RunVerb) });
                if (runMethod != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
