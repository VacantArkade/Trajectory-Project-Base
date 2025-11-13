using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTurret : MonoBehaviour
{
    [SerializeField] LayerMask targetLayer;
    [SerializeField] GameObject crosshair;
    [SerializeField] float baseTurnSpeed = 3;
    [SerializeField] GameObject gun;
    [SerializeField] Transform turretBase;
    [SerializeField] Transform barrelEnd;
    [SerializeField] LineRenderer line;

    List<Vector3> laserPoints = new List<Vector3>();

    [SerializeField] int maxBounces = 1;
    [SerializeField] float maxDistance = 1000f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        TrackMouse();
        TurnBase();

        laserPoints.Clear();
        laserPoints.Add(barrelEnd.position);

        if(Physics.Raycast(barrelEnd.position, barrelEnd.forward, out RaycastHit hit, 1000.0f, targetLayer))
        {
            laserPoints.Add(hit.point);
        }

        line.positionCount = laserPoints.Count;
        for(int i = 0; i < line.positionCount; i++)
        {
            line.SetPosition(i, laserPoints[i]);
        }
    }

    void LateUpdate()
    {
        if (line == null || barrelEnd == null)
            return;

        List<Vector3> points = new List<Vector3>();
        Vector3 origin = barrelEnd.position;
        Vector3 dir = barrelEnd.forward.normalized;
        float remaining = maxDistance;

        points.Add(origin);

        int bounces = 0;
        while (remaining > 0f && bounces <= maxBounces)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, remaining, targetLayer))
            {
                points.Add(hit.point);

                if (bounces == maxBounces)
                    break;

                float dot = dir.x * hit.normal.x + dir.y * hit.normal.y + dir.z * hit.normal.z;
                Vector3 reflect = new Vector3(
                    dir.x - 2f * dot * hit.normal.x,
                    dir.y - 2f * dot * hit.normal.y,
                    dir.z - 2f * dot * hit.normal.z
                );

                float mag = Mathf.Sqrt(reflect.x * reflect.x + reflect.y * reflect.y + reflect.z * reflect.z);
                if (mag > 1e-6f)
                {
                    reflect.x /= mag;
                    reflect.y /= mag;
                    reflect.z /= mag;
                }
                else
                {
                    break;
                }

                float used = Vector3.Distance(points[points.Count - 1], origin);
                remaining -= used;
                origin = hit.point + reflect;
                dir = reflect;
                bounces++;
            }
            else
            {
                points.Add(origin + dir * remaining);
                break;
            }
        }

        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }
    }

    void TrackMouse()
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if(Physics.Raycast(cameraRay, out hit, 1000, targetLayer ))
        {
            crosshair.transform.forward = hit.normal;
            crosshair.transform.position = hit.point + hit.normal * 0.1f;
        }
    }

    void TurnBase()
    {
        Vector3 directionToTarget = (crosshair.transform.position - turretBase.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, directionToTarget.y, directionToTarget.z));
        turretBase.transform.rotation = Quaternion.Slerp(turretBase.transform.rotation, lookRotation, Time.deltaTime * baseTurnSpeed);
    }
}
