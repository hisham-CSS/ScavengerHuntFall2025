using UnityEngine;

public class TriggerButtonFade : MonoBehaviour
{
    public Animator titleAnim;
    public Animator buttonAnim;

    // Update is called once per frame
    void Update()
    {
        AnimatorStateInfo buttotnAnimStateInfo = buttonAnim.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo titleAnimStateInfo = titleAnim.GetCurrentAnimatorStateInfo(0);

        if (buttotnAnimStateInfo.IsName("FadeIn")) return;

        if (titleAnimStateInfo.normalizedTime >= 1)
        {
            buttonAnim.SetTrigger("StartFade");
        }
    }
}
