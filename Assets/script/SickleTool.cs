using UnityEngine;
using System.Collections;

public class SickleTool : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackRange = 2.5f;
    public float attackCooldown = 0.5f;
    private bool _isAttacking = false;

    [Header("휘두르기 연출")]
    public Transform sickleVisual; // 실제 낫 모델링의 Transform
    public Vector3 swingRotation = new Vector3(60, 0, 0); // 휘두를 각도
    public float swingSpeed = 10f;

    void Update()
    {
        // 마우스 왼쪽 클릭 시 휘두르기 (UI가 닫혀있을 때만)
        if (Input.GetMouseButtonDown(0) && !_isAttacking)
        {
            StartCoroutine(SwingSickle());
        }
    }

    IEnumerator SwingSickle()
    {
        _isAttacking = true;

        // 1. 공격 판정 (레이캐스트)
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, attackRange))
        {
            Barnacle barnacle = hit.collider.GetComponent<Barnacle>();
            if (barnacle != null)
            {
                barnacle.TakeDamage(1);
            }
        }

        // 2. 휘두르는 움직임 (Rotation 60도 이동)
        Quaternion startRot = sickleVisual.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(swingRotation);

        // 내려치기
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            sickleVisual.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / 0.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 돌아오기
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            sickleVisual.localRotation = Quaternion.Slerp(endRot, startRot, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        sickleVisual.localRotation = startRot;
        yield return new WaitForSeconds(attackCooldown);
        _isAttacking = false;
    }
}