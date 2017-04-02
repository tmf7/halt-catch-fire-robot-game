using UnityEngine;


public class TargetDragScript : MonoBehaviour
{

    public float damping = 1f;
    public float frequency = 5f;
	private Robot grabbedRobot;
    private HingeJoint2D hingeJoint;

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

			hingeJoint = body.gameObject.AddComponent<HingeJoint2D>();
	//		hingeJoint.dampingRatio = damping;
	//		hingeJoint.frequency = frequency;

			hingeJoint.anchor = hingeJoint.transform.InverseTransformPoint(worldPosition);
        }
		else if (Input.GetMouseButtonUp(0))
        {
		//	if (grabbedRobot != null) {
			Destroy (hingeJoint);
			hingeJoint = null;
			grabbedRobot.grabbed = false;
			return;
		//	}
        }

		if (hingeJoint)
        {
			hingeJoint.
		//	hingeJoint.target = worldPosition;
        }
    }
}
