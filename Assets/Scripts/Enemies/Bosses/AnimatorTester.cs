using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatorTester : MonoBehaviour
{
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
    }

    // Update is called once per frame
    void Update()
    {
       if (InputSystem.GetDevice<Keyboard>().spaceKey.wasPressedThisFrame)
        {
            animator.SetTrigger("Raise");
        } 
    }
}
