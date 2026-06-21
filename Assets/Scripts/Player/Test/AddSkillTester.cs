using UnityEngine;

public class AddSkillTester : MonoBehaviour
{
    [SerializeField] private string skillIDToAdd;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameProgressManager.Instance.Acquire(skillIDToAdd);
            Debug.Log($"Skill '{skillIDToAdd}' acquired!");
        }
    }
}
