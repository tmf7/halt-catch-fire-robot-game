using UnityEngine;


public class TargetDragScript : MonoBehaviour
{


    public float damping = 1f;
    public float frequency = 5f;
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

            //change this to 'Robot' when using robot objects;
            var tagNameToBePickedUp = "Box";
            if (collider.tag != tagNameToBePickedUp)
                return;

          

            targetJoint = body.gameObject.AddComponent<TargetJoint2D>();
            targetJoint.dampingRatio = damping;
            targetJoint.frequency = frequency;

            targetJoint.anchor = targetJoint.transform.InverseTransformPoint(worldPosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Destroy(targetJoint);
            targetJoint = null;
            return;
        }

        if (targetJoint)
        {
            targetJoint.target = worldPosition;
        }
    }
}
