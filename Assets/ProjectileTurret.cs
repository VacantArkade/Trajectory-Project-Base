using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileTurret : MonoBehaviour
{
    [SerializeField] float projectileSpeed = 1;
    [SerializeField] Vector3 gravity = new Vector3(0, -9.8f, 0);
    [SerializeField] LayerMask targetLayer;
    [SerializeField] GameObject crosshair;
    [SerializeField] float baseTurnSpeed = 3;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] GameObject gun;
    [SerializeField] Transform turretBase;
    [SerializeField] Transform barrelEnd;
    [SerializeField] LineRenderer line;
    [SerializeField] bool useLowAngle;

    List<Vector3> points = new List<Vector3>();

    [SerializeField] float integrateStep = 0.02f;
    [SerializeField] float integrateMaxTime = 5f;
    [SerializeField] float integrateSphereRadius = 0.05f;

    bool projectGravity = false;
    Vector3 prefabGravity = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        if (projectilePrefab != null)
        {
            var rb = projectilePrefab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                projectGravity = rb.useGravity;
                prefabGravity = Physics.gravity;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        TrackMouse();
        TurnBase();
        RotateGun();

        DrawTrajectory();

        if (Input.GetButtonDown("Fire1"))
            Fire();
    }

    void Fire()
    {
        GameObject projectile = Instantiate(projectilePrefab, barrelEnd.position, gun.transform.rotation);
        projectile.GetComponent<Rigidbody>().linearVelocity = projectileSpeed * barrelEnd.transform.forward;
    }

    void TrackMouse()
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if(Physics.Raycast(cameraRay, out hit, 1000, targetLayer))
        {
            crosshair.transform.forward = hit.normal;
            crosshair.transform.position = hit.point + hit.normal * 0.1f;
            //Debug.Log("hit ground");
        }
    }

    void TurnBase()
    {
        Vector3 directionToTarget = (crosshair.transform.position - turretBase.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        turretBase.transform.rotation = Quaternion.Slerp(turretBase.transform.rotation, lookRotation, Time.deltaTime * baseTurnSpeed);
    }

    void RotateGun()
    {
        float? angle = CalculateTrajectory(crosshair.transform.position, useLowAngle);
        if (angle != null)
            gun.transform.localEulerAngles = new Vector3(360f - (float)angle, 0, 0);
    }

    float? CalculateTrajectory(Vector3 target, bool useLow)
    {
        Vector3 targetDir = target - barrelEnd.position;
        
        float y = targetDir.y;
        targetDir.y = 0;

        float x = targetDir.magnitude;

        float v = projectileSpeed;
        float v2 = Mathf.Pow(v, 2);
        float v4 = Mathf.Pow(v, 4);
        float g = gravity.y;
        float x2 = Mathf.Pow(x, 2);

        float underRoot = v4 - g * ((g * x2) + (2 * y * v2));

        if (underRoot >= 0)
        {
            float root = Mathf.Sqrt(underRoot);
            float highAngle = v2 + root;
            float lowAngle = v2 - root;

            if (useLow)
                return (Mathf.Atan2(lowAngle, g * x) * Mathf.Rad2Deg);
            else
                return (Mathf.Atan2(highAngle, g * x) * Mathf.Rad2Deg);
        }
        else
            return null;
    }

    

    void DrawTrajectory()
    {
        if (line == null || barrelEnd == null)
            return;

        points.Clear();

        Vector3 startPos = barrelEnd.position;
        Vector3 startVel = barrelEnd.forward * projectileSpeed;
        Vector3 usedGravity = projectGravity ? Physics.gravity : gravity;

        float elapsed = 0f;
        Vector3 pos = startPos;
        Vector3 vel = startVel;
        Vector3 prev = pos;

        points.Add(pos);

        while (elapsed < integrateMaxTime)
        {
            float dt = Mathf.Min(integrateStep, integrateMaxTime - elapsed);

            vel += usedGravity * dt;
            pos += vel * dt;

            points.Add(pos);

            Vector3 segment = pos - prev;
            float segLen = segment.magnitude;
            if (segLen > 1e-6f)
            {
                if (Physics.SphereCast(prev, integrateSphereRadius, segment.normalized, out RaycastHit hit, segLen, targetLayer))
                {
                    points[points.Count - 1] = hit.point;
                    break;
                }
            }

            prev = pos;
            elapsed += dt;
        }

        if (points.Count == 0)
        {
            line.positionCount = 0;
            return;
        }

        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
    }
}
