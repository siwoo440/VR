using UnityEngine;

namespace AtelierVerse.World
{
    /// <summary>
    /// 블록을 놓을 자리와 방향을 다듬는 계산(15일차): 돌리는 각도의 단계와 맞추기 도우미.
    /// 맞추기 도우미는 끈 것이 기본이며, 켜면 바닥에서는 모눈에, 놓인 블록 위에서는 그 블록에 나란히 붙는 자리로 당긴다.
    /// 장면과 무관해 편집 모드 테스트로 검사한다.
    /// </summary>
    public static class PlacementMath
    {
        /// <summary>한 번에 돌리는 각도(도). 좌우로만 돌린다.</summary>
        public const float YawStep = 15f;

        /// <summary>맞추기 도우미의 단계. 0은 끔이고, 나머지는 맞추는 간격(블록 한 변이 1)이다.</summary>
        public static readonly float[] SnapSteps = { 0f, 1f, 0.5f, 0.25f };

        private static readonly string[] SnapLabels = { "끔", "1칸", "1/2칸", "1/4칸" };

        private const float AxisAligned = 0.99f;

        public static int SnapLevelCount => SnapSteps.Length;

        /// <summary>단계의 번호를 범위 안으로 맞춘다. 범위 밖이면 끔(0)이다.</summary>
        public static int ClampSnapLevel(int level)
        {
            return level >= 0 && level < SnapSteps.Length ? level : 0;
        }

        /// <summary>다음 단계. 마지막 다음은 다시 끔이다.</summary>
        public static int NextSnapLevel(int level)
        {
            return (ClampSnapLevel(level) + 1) % SnapSteps.Length;
        }

        public static float SnapStepOf(int level)
        {
            return SnapSteps[ClampSnapLevel(level)];
        }

        /// <summary>화면에 보이는 단계의 이름(끔, 1칸, 1/2칸, 1/4칸).</summary>
        public static string SnapLabelOf(int level)
        {
            return SnapLabels[ClampSnapLevel(level)];
        }

        /// <summary>각도를 0 이상 360 미만으로 맞춘다.</summary>
        public static float NormalizeYaw(float yaw)
        {
            yaw = Mathf.Repeat(yaw, 360f);
            yaw = Mathf.Round(yaw * 1000f) / 1000f;
            return yaw >= 360f ? 0f : yaw;
        }

        /// <summary>각도를 steps 단계만큼 돌린다. 양수는 위에서 보아 시계 방향이다.</summary>
        public static float StepYaw(float yaw, int steps)
        {
            return NormalizeYaw(yaw + steps * YawStep);
        }

        public static Quaternion YawRotation(float yaw)
        {
            return Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>방향에서 좌우로 돈 각도만 꺼낸다. 앞(+z)이 수평으로 향하는 쪽을 본다.</summary>
        public static float YawOf(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return 0f;

            return NormalizeYaw(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// 값 하나를 모눈에 맞춘다. 블록의 가운데가 오는 자리이므로, 간격이 1이면 칸의 가운데(…, -0.5, 0.5, 1.5, …)다.
        /// 간격이 1/2이면 그 사이가 하나, 1/4이면 셋 더 생긴다.
        /// </summary>
        public static float SnapCenter(float value, float step)
        {
            if (step <= 0f) return value;

            float half = GridMath.DefaultCellSize * 0.5f;
            return Mathf.Round((value - half) / step) * step + half;
        }

        /// <summary>
        /// 바닥이나 블록이 아닌 물체의 면을 가리켰을 때의 자리. 면 위에 얹은 자리(면에서 반 변 바깥)를 모눈에 맞춘다.
        /// 면이 축과 나란하면 면에 닿는 쪽은 그대로 두어 면에 붙어 있게 하고, 나머지 두 쪽만 맞춘다.
        /// </summary>
        public static Vector3 SnapToGrid(Vector3 point, Vector3 normal, float step)
        {
            Vector3 outward = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Vector3 rest = point + outward * (GridMath.DefaultCellSize * 0.5f);
            if (step <= 0f) return rest;

            int axis = DominantAxis(outward);
            bool aligned = Mathf.Abs(outward[axis]) >= AxisAligned;

            var snapped = new Vector3(SnapCenter(rest.x, step), SnapCenter(rest.y, step), SnapCenter(rest.z, step));
            if (aligned) snapped[axis] = rest[axis];
            return snapped;
        }

        /// <summary>
        /// 놓인 블록의 면을 가리켰을 때의 자리. 그 블록을 기준으로 삼아, 가리킨 면 쪽으로 정확히 한 변 떨어진 곳에 둔다.
        /// 면 위에서는 그 블록의 가운데에서 간격의 배수만큼 떨어진 자리로 당긴다(간격이 1이면 바로 옆에 나란히).
        /// 블록이 돌아가 있으면 돌아간 방향을 따라 붙는다.
        /// </summary>
        public static Vector3 SnapToBlock(Vector3 blockPosition, Quaternion blockRotation, Vector3 point, Vector3 normal, float step)
        {
            if (step <= 0f) return SnapToGrid(point, normal, 0f);

            Quaternion inverse = Quaternion.Inverse(blockRotation);
            Vector3 local = inverse * (point - blockPosition);
            Vector3 localNormal = inverse * (normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up);

            int axis = DominantAxis(localNormal);
            var offset = new Vector3(
                Mathf.Round(local.x / step) * step,
                Mathf.Round(local.y / step) * step,
                Mathf.Round(local.z / step) * step);
            offset[axis] = Mathf.Sign(localNormal[axis]) * GridMath.DefaultCellSize;

            return blockPosition + blockRotation * offset;
        }

        private static int DominantAxis(Vector3 direction)
        {
            float x = Mathf.Abs(direction.x);
            float y = Mathf.Abs(direction.y);
            float z = Mathf.Abs(direction.z);
            if (y >= x && y >= z) return 1;
            return x >= z ? 0 : 2;
        }
    }
}
