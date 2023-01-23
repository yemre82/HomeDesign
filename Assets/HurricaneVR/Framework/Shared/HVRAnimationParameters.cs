using System.Collections.Generic;

namespace HurricaneVR.Framework.Shared
{
    public static class HVRAnimationParameters
    {
        private static readonly Dictionary<string, float> _leftFloatParams = new Dictionary<string, float>();
        private static readonly Dictionary<string, float> _rightFloatParams = new Dictionary<string, float>();
        private static readonly Dictionary<string, bool> _leftBooleanParams = new Dictionary<string, bool>();
        private static readonly Dictionary<string, bool> _rightBooleanParams = new Dictionary<string, bool>();

        public static void SetFloatParameter(HVRHandSide side, string parameter, float value)
        {
            var map = side == HVRHandSide.Left ? _leftFloatParams : _rightFloatParams;
            map[parameter] = value;
        }

        public static float GetFloatParameter(HVRHandSide side, string parameter)
        {
            var map = side == HVRHandSide.Left ? _leftFloatParams : _rightFloatParams;
            map.TryGetValue(parameter, out float value);
            return value;
        }

        public static void SetBoolParameter(HVRHandSide side, string parameter, bool value)
        {
            var map = side == HVRHandSide.Left ? _leftBooleanParams : _rightBooleanParams;
            map[parameter] = value;
        }

        public static bool GetBoolParameter(HVRHandSide side, string parameter)
        {
            var map = side == HVRHandSide.Left ? _leftBooleanParams : _rightBooleanParams;
            map.TryGetValue(parameter, out bool value);
            return value;
        }
    }
}
