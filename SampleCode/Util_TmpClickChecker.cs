using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using TMPro;
using System.Reflection;
using System;

public class Util_TmpClickChecker : MonoBehaviour, IPointerDownHandler 
{
    public UnityEvent<string> _InspectorEvent;
    UnityEvent<string> _event = new UnityEvent<string>();
    TextMeshProUGUI _tmp;




    private void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();


        MethodInfo info = UnityEventBase.GetValidMethodInfo(_InspectorEvent.GetPersistentTarget(0), _InspectorEvent.GetPersistentMethodName(0), new Type[] { typeof(string) });
        UnityAction<string> action = (data) => info.Invoke(_InspectorEvent.GetPersistentTarget(0), new object[] { data });

        _event.AddListener(action);

    }


    public void OnPointerDown(PointerEventData eventData)
    {


        int index = TMP_TextUtilities.FindIntersectingWord(_tmp, Input.mousePosition, null);
        if (index != -1)
        {
            string[] spl = _tmp.text.Split(' ', '\n');

            _event.Invoke(spl[index]);
        }
    }

}
