using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Util_TouchEffect : MonoBehaviour
{
    [SerializeField] RectTransform _rt;
    [SerializeField] GameObject _objEffect_Origin;
    [SerializeField] int _pullingCnt;

    List<RectTransform> _listObj = new List<RectTransform>();
    int index=0;

    private void Awake()
    {
        var list = FindObjectsOfType<Util_TouchEffect>();
        if (list.Length > 1)
            Destroy(this.gameObject);
        else
            DontDestroyOnLoad(this.gameObject);

        for (int i = 0; i < _pullingCnt; i++)
        {
            GameObject obj = Instantiate(_objEffect_Origin, _rt);
            obj.SetActive(false);
            _listObj.Add(obj.GetComponent<RectTransform>());
        }

    }


    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            ShowEffect();
    }

    void ShowEffect()
    {
        // ��ġ Ȯ��
        Vector2 targetPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, Input.mousePosition, null, out targetPos);

        // ������Ʈ Ȱ�� : ���� ��Ȱ��
        RectTransform target = _listObj[index++];
        target.anchoredPosition = targetPos;
        target.gameObject.SetActive(false);
        target.gameObject.SetActive(true);

        // �ε��� üũ
        if (index >= _pullingCnt)
            index = 0;
    }

}
