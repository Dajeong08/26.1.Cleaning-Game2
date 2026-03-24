using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class SubmarinePart : MonoBehaviour
{
    [Header("마스크 설정")]
    public int maskSize = 512;

    private Texture2D _maskTex;
    private Color32[] _pixels;
    private Material _mat;
    private bool _isDirty;

    private int _totalPixels;
    private int _cleanedPixels;

    [HideInInspector] public float partProgress = 0f;
    public System.Action onProgressChanged;

    void Start()
    {
        _mat = GetComponent<Renderer>().material;

        _maskTex = new Texture2D(maskSize, maskSize, TextureFormat.R8, false);
        _pixels = new Color32[maskSize * maskSize];

        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] = new Color32(0, 0, 0, 255);

        _maskTex.SetPixels32(_pixels);
        _maskTex.Apply();

        _mat.SetTexture("_MaskTex", _maskTex);
        _totalPixels = maskSize * maskSize;
        _cleanedPixels = 0;

        CalculateProgress();
    }

    public void CleanByWorldPos(Vector3 worldPos, float worldRadius, float speed)
    {
        RaycastHit hit;
        Vector3 direction = (worldPos - Camera.main.transform.position).normalized;

        if (Physics.Raycast(worldPos - direction * 0.5f, direction, out hit, 1.0f))
        {
            if (hit.collider.gameObject == gameObject)
            {
                float uvRadius = worldRadius * 0.02f;
                Clean(hit.textureCoord, uvRadius, speed);
            }
        }
    }

    public void Clean(Vector2 uv, float radius, float speed)
    {
        int cx = Mathf.RoundToInt(uv.x * (maskSize - 1));
        int cy = Mathf.RoundToInt(uv.y * (maskSize - 1));
        int r = Mathf.RoundToInt(radius * maskSize);
        int r2 = r * r;

        PaintCircle(cx, cy, r, r2, speed);

        if (cx - r < 0) PaintCircle(cx + maskSize, cy, r, r2, speed);
        if (cx + r >= maskSize) PaintCircle(cx - maskSize, cy, r, r2, speed);

        if (_isDirty)
        {
            _maskTex.SetPixels32(_pixels);
            _maskTex.Apply();
            _isDirty = false;

            CalculateProgress();
        }
    }

    private void PaintCircle(int cx, int cy, int r, int r2, float speed)
    {
        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                if (dx * dx + dy * dy > r2) continue;

                int px = cx + dx;
                int py = cy + dy;
                if (px < 0 || px >= maskSize || py < 0 || py >= maskSize) continue;

                int idx = py * maskSize + px;
                byte cur = _pixels[idx].r;

                if (cur < 255)
                {
                    int add = Mathf.RoundToInt(speed * Time.deltaTime * 255f);
                    int next = Mathf.Clamp(cur + add, 0, 255);
                    _pixels[idx] = new Color32((byte)next, 0, 0, 255);

                    if (next >= 230 && cur < 230)
                        _cleanedPixels++;

                    _isDirty = true;
                }
            }
        }
    }

    private void CalculateProgress()
    {
        float prevProgress = partProgress;
        partProgress = (_cleanedPixels / (float)_totalPixels) * 100f;
        partProgress = Mathf.Clamp(partProgress, 0f, 100f);

        if (Mathf.Abs(prevProgress - partProgress) > 0.01f)
        {
            onProgressChanged?.Invoke();
        }
    }

    public void RevealDirt(float duration)
    {
        StartCoroutine(RevealRoutine(duration));
    }

    private IEnumerator RevealRoutine(float duration)
    {
        if (_mat == null) _mat = GetComponent<Renderer>().material;
        _mat.SetFloat("_IsScanning", 1f);
        yield return new WaitForSeconds(duration);
        _mat.SetFloat("_IsScanning", 0f);
    }

    void OnDestroy()
    {
        if (_maskTex != null) Destroy(_maskTex);
    }
}
