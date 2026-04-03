using UnityEngine;

public class ResetAnimationPositionBehavior : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //HeightAndSpriteResizeSystem height = animator.GetComponentInParent<HeightAndSpriteResizeSystem>();
        //Collider2D collider = height.Parent.GetComponent<Collider2D>();
        //animator.rootPosition = height.pivotPos;

        //height.Sprite.position = collider.bounds.center;
        //Vector3 animVect = animator.GetComponentInParent<HeightAndSpriteResizeSystem>().pivotPos;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //HeightAndSpriteResizeSystem height = animator.GetComponentInParent<HeightAndSpriteResizeSystem>();
        //Collider2D collider = height.Parent.GetComponent<Collider2D>();

        //AnimatorClipInfo clip = animator.GetCurrentAnimatorClipInfo(0);
        //clip.clip.SampleAnimation(animator.gameObject, 1f);
        
        //Vector3 animVect = animator.GetComponentInParent<HeightAndSpriteResizeSystem>().pivotPos;
        //Transform controller = animator.GetComponentInParent<MovementController>().transform;
        //animVect.x = animVect.x * controller.transform.localScale.x;
        //animVect.y = animVect.y * controller.transform.localScale.y;
        //animator.rootPosition = height.pivotPos;
        //eight.Sprite.position = collider.bounds.center;
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Implement code that processes and affects root motion
        //HeightAndSpriteResizeSystem height = animator.GetComponentInParent<HeightAndSpriteResizeSystem>();
        //animator.rootPosition = height.pivotPos;
    }

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
