using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;

public class Util_FadeOnUI : MonoBehaviour
{
    [SerializeField] float _fadeSpeed = 15;

    CanvasGroup _cg;


    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();

        if (_cg == null)
            _cg = gameObject.AddComponent<CanvasGroup>();
    }


    public void OnEnable()
    {
        _cg.alpha = 0;

        FadeOn().Forget();
    }

    private void OnDisable()
    {
        _cg.alpha = 1;
    }

    public void OnDestroy()
    {
        _cg.alpha = 1;
    }




    async UniTaskVoid FadeOn()
    {

        while(_cg.alpha < 1)
        {
            _cg.alpha = Mathf.Lerp(_cg.alpha , 1.1f, _fadeSpeed * Time.deltaTime);

            await UniTask.NextFrame();
        }


    }
}
