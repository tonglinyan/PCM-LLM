using Simulation;
using UnityEngine;

public class Box : ObjectOfInterest
{
    [SerializeField] private GameObject reward;
    private Animator animator;
    //private MeshRenderer meshRenderer;
    private bool isBoxOpened = false;
    public bool Interactable { get; set; } 


    private void Start()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        Interactable = false;
    }

    private new void Update()
    {
        base.Update();
    }

    public void RemoveReward()
    {
        reward.SetActive(false);
        //meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Open()
    {
        if (Interactable && !isBoxOpened && !Manager.Singleton.boxOpened) {
            //meshRenderer.enabled = false;
            animator.SetTrigger("Open");
            isBoxOpened = true;
            Debug.Log("Box opened");
            Manager.Singleton.OpenBox(this);
        }
    }

    public void Close()
    {
        if (Interactable && isBoxOpened) { 
            //meshRenderer.enabled = true;
            animator.SetTrigger("Close");
            isBoxOpened = false;
            Manager.Singleton.boxOpened = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Manager.Singleton.OpenBox(this);
        //StartCoroutine(Manager.Singleton.End());
    }
}

