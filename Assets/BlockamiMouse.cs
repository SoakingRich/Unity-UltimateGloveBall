using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Oddworm.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

[DefaultExecutionOrder(2000)]
public class BlockamiMouse : MonoBehaviour
{
    public Camera mainCamera;
    public Vector3 worldMousePosition;
    public Vector3 mouseRayDirection;
    public bool Ignore = false;
    
    [SerializeField] private InputActionProperty MouseAction;
    [SerializeField] private InputActionProperty MouseClickAction;

    
    
    private T ReadAction<T>(InputActionProperty property) where T : struct =>
         property.action.ReadValue<T>() ;
    private bool IsPressed(InputActionProperty property) =>
         property.action.IsPressed();

    
    
    
    
    
    
    void Awake()
    {

        if (!(XRSettings.loadedDeviceName?.Trim() is ("MockHMD Display" or "" or null)))
        {
            enabled = false;
            Ignore = true;
        }
        

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        MouseAction.action.Enable();
        MouseClickAction.action.Enable();
    
    }
    
    
    

    void Update()
    {
        if (Ignore) return;
        if (mainCamera == null) return;
        
   
        Vector2 mouseScreenPos = ReadAction<Vector2>(MouseAction);
        
        // Set a fixed Z depth for conversion (important for 3D)
        Vector3 screenPos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane);
        
        worldMousePosition = mainCamera.ScreenToWorldPoint(screenPos);
    //    Debug.Log("WorldSpace mouse position is " + worldMousePosition);
        
        //DbgDraw.WireSphere(worldMousePosition,Quaternion.identity,Vector3.one*0.01f, Color.yellow,0.1f,false);
        
        Ray mouseRay = mainCamera.ScreenPointToRay(mouseScreenPos);
        mouseRayDirection = mouseRay.direction;
        
    //   Debug.DrawRay(mouseRay.origin, mouseRay.direction * 10, Color.red, 0.1f);
    //    DbgDraw.Ray(mouseRay,Color.yellow,0.1f,false);

        
        
       
        
        RaycastHit[] hits = Physics.RaycastAll(mouseRay);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.isTrigger)
            {
                SnapZone snapzone = hit.collider.GetComponent<SnapZone>();
                if (snapzone != null)
                {
                 //   DbgDraw.WireSphere(hit.point, Quaternion.identity, Vector3.one * 0.01f, Color.yellow, 0.1f, false);
                    
                    var allTPE = FindObjectsOfType<TriggerPinchEvents>().Where(s => s.IsRight == true).ToArray();
                    if (allTPE.Length > 0)
                    {
                        allTPE[0].transform.position = hit.transform.position;
                        Collider c = allTPE[0].GetComponent<Collider>();
                        snapzone.DoTriggerStay(c);


                        snapzone.OwningGrid.PointerUI.FPSControlled = true;
                        snapzone.OwningGrid.PointerUI.LerpTargetPosition = snapzone.transform.position;
                    }

                }
                
             //   Debug.Log("Ray hitting trigger " + hit.collider.gameObject);
            }
            else
            {
            //    Debug.Log("Ray hitting " + hit.collider.gameObject);
            }
        }
        
        
        
     



    }
}
