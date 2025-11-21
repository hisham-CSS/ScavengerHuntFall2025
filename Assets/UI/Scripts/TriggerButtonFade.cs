using UnityEngine;

public class TriggerButtonFade : MonoBehaviour
{
    public Animator titleAnim;
    public Animator buttonAnim;

    // Update is called once per frame
    void Update()
    {
        AnimatorStateInfo buttonAnimStateInfo = buttonAnim.GetCurrentAnimatorStateInfo(0);
        if (buttonAnimStateInfo.IsName("FadeIn")) return;
        
        AnimatorStateInfo titleAnimStateInfo = titleAnim.GetCurrentAnimatorStateInfo(0);
        if (titleAnimStateInfo.normalizedTime >= 1) buttonAnim.SetTrigger("StartFade");
    }
}
