using UnityEngine;


public class TargetDragScript : MonoBehaviour
{

    public float damping = 1f;
    public float frequency = 5f;
	private Robot grabbedRobot;
    private TargetJoint2D targetJoint;

    void Update()
    {
        var mousePos = Input.mousePosition;
        mousePos.z = 10;

        var worldPosition = Camera.main.ScreenToWorldPoint(mousePos);

        if (Input.GetMouseButtonDown(0))
        {

            RaycastHit2D rayHit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));

            var collider = rayHit.collider;
            if (!collider)
                return;

            var body = collider.attachedRigidbody;
            if (!body)
                return;

			if (collider.gameObject.tag != "Robot")
                return;          

			// stop the robot from pathfinding/following while grabbed
			grabbedRobot = collider.gameObject.GetComponent<Robot> ();
			grabbedRobot.grabbed = true;

            targetJoint = body.gameObject.AddComponent<TargetJoint2D>();
            targetJoint.dampingRatio = damping;
            targetJoint.frequency = frequency;

            targetJoint.anchor = targetJoint.transform.InverseTransformPoint(worldPosition);
        }
		else if (Input.GetMouseButtonUp(0))
        {
		//	if (grabbedRobot != null) {
				Destroy (targetJoint);
				targetJoint = null;
			grabbedRobot.grabbed = false;
				return;
		//	}
        }

        if (targetJoint)
        {
            targetJoint.target = worldPosition;
        }
    }
}
