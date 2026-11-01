using UnityEngine;

namespace DTT.AreaOfEffectRegions
{
    /// <summary>
    /// An implementation of <see cref="LineRegionBase"/> with added textures.
    /// </summary>
    [ExecuteAlways]
    public class LineRegion : LineRegionBase
    {
        /// <summary>
        /// The body of the line.
        /// </summary>
        [SerializeField]
        private MeshRenderer _bodyRenderer;
        
        /// <summary>
        /// The end point, head, of the line.
        /// </summary>
        [SerializeField]
        private MeshRenderer _headRenderer;
        
        /// <summary>
        /// The body of the line.
        /// </summary>
        [SerializeField]
        private Transform _bodyTransform;
        
        /// <summary>
        /// The end point, head, of the line.
        /// </summary>
        [SerializeField]
        private Transform _headTransform;
        
        /// <summary>
        /// The shader ID of the _FillProgress property.
        /// </summary>
        private static readonly int ProgressShaderID = Shader.PropertyToID("_FillProgress");

        /// <summary>
        /// Updates the position of the head and the body according to the line data.
        /// </summary>
        private void Update()
        {
            if (_bodyTransform == null || _headTransform == null)
                return;
            
            // [변경] PROJECT3D : 머리 길이가 폭(Width)을 따라가게 (원래는 폭 1 기준 고정이라 폭이 1보다 크면 머리가 납작하게 눌려 보였다)
            //  머리 z = min(Length / 2, Width), 머리 실제 길이 = z × 2 (머리 메쉬 y 2배), 몸통 = 나머지 길이
            //  Width = 1이면 원래 계산(머리 min(Length / 2, 1), 몸통 Length - 2)과 같다
            float headScaleZ = Mathf.Min(Length / 2f, Width);

            // Sets body angle and scale.
            _bodyTransform.localEulerAngles = new Vector3(_bodyTransform.eulerAngles.x, Angle, _bodyTransform.eulerAngles.z);
            _bodyTransform.localScale = new Vector3(Width, _bodyTransform.localScale.y, Mathf.Max(Length - headScaleZ * 2f, 0)); // [변경] Length - 2 → 머리 길이만큼 뺌

            // Sets head angle, scale and position.
            _headTransform.localScale = new Vector3(Width, _headTransform.localScale.y, headScaleZ); // [변경] Mathf.Min((Length / 2), 1) → headScaleZ
            _headTransform.position = _bodyTransform.position + _bodyTransform.forward * _bodyTransform.localScale.z;
            _headTransform.eulerAngles = _bodyTransform.eulerAngles;

            float bodyPart = _bodyTransform.localScale.z / Length;
            _bodyRenderer.sharedMaterial.SetFloat(ProgressShaderID, Mathf.InverseLerp(0, bodyPart, FillProgress));
            _headRenderer.sharedMaterial.SetFloat(ProgressShaderID, Mathf.InverseLerp(bodyPart, 1, FillProgress));
        }
    }
}