using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using System.Threading;

public class Util_HpSlider : MonoBehaviour
{

    [SerializeField] SlicedFilledImage _imgHpGauge_front;
    [SerializeField] SlicedFilledImage _imgHpGauge_back;

    [SerializeField] Color _colDamageColor = new Color(0.7f, 0, 0);
    [SerializeField] Color _colHealColor = new Color(0.8f, 1, 0.8f);

    // 게이지 조작용 데이터
    CancellationTokenSource _hpGaugeToken;
    bool _isChangeDownHP = false;
    bool _isChangeUpHP = false;
    float _hpGaugeTarget = 0;


    public void GaugeReset(float per = 1)
    {
        _imgHpGauge_front.fillAmount = per;
        _imgHpGauge_back.fillAmount = per;
    }

  
    public void ChangeHpToPer(float per)
    {
        if (_imgHpGauge_front.fillAmount > per)
            Damage(per);
        else if (_imgHpGauge_front.fillAmount < per)
            Heal(per);

    }

    public void Damage( float targetPercent)
    {

        // 게이지 목표점
        _hpGaugeTarget = targetPercent;

        // 회복 연출중
        if (_isChangeUpHP)
        {
            // 회복 연출중인 량이 데미지보다 많으면 최종치만 낮추고 리턴
            if (_imgHpGauge_front.fillAmount < _hpGaugeTarget)
            {
                _imgHpGauge_back.fillAmount = _hpGaugeTarget;
                return;
            }

            _hpGaugeToken.Cancel();
            _hpGaugeToken.Dispose();
            _hpGaugeToken = null;
            _isChangeUpHP = false;
        }

        // 앞 게이지 선 감소
        _imgHpGauge_front.fillAmount = _hpGaugeTarget;

        // 뒤 게이지 색 변경
        _imgHpGauge_back.color = _colDamageColor;

        // 감소 연출 시작
        if (!_isChangeDownHP)
        {
            _isChangeDownHP = true;
            _hpGaugeToken = new CancellationTokenSource();
            ChangeHpGauge(true).Forget();
        }


    }

    public void Heal(float targetPercent)
    {
        // 게이지 목표점
        _hpGaugeTarget = targetPercent;

        // 감소 연출중
        if (_isChangeDownHP)
        {
            // 감소 연출중인 량이 회복보다 많으면 최종치만 낮추고 리턴
            if (_imgHpGauge_back.fillAmount > _hpGaugeTarget)
            {
                _imgHpGauge_front.fillAmount = _hpGaugeTarget;
                return;
            }

            _hpGaugeToken.Cancel();
            _hpGaugeToken.Dispose();
            _hpGaugeToken = null;
            _isChangeDownHP = false;

        }

        // 뒤 게이지 선 증가
        _imgHpGauge_back.fillAmount = _hpGaugeTarget;

        // 뒤 게이지 색 변경
        _imgHpGauge_back.color = _colHealColor;

        // 증가 연출 시작
        if (!_isChangeUpHP)
        {
            _isChangeUpHP = true;
            _hpGaugeToken = new CancellationTokenSource();
            ChangeHpGauge(false).Forget();
        }
    }


    async UniTaskVoid ChangeHpGauge(bool isDown)
    {

        if (isDown)
        {
            // 뒤 게이지 lerp 감소
            while (_imgHpGauge_back.fillAmount > _hpGaugeTarget)
            {
                _imgHpGauge_back.fillAmount = Mathf.Lerp(_imgHpGauge_back.fillAmount, _hpGaugeTarget - 0.05f, Time.deltaTime * 1);
                await UniTask.NextFrame(_hpGaugeToken.Token);
            }
        }
        else
        {
            // 앞 게이지 lerp 증가
            while (_imgHpGauge_front.fillAmount < _hpGaugeTarget)
            {
                _imgHpGauge_front.fillAmount = Mathf.Lerp(_imgHpGauge_front.fillAmount, _hpGaugeTarget + 0.05f, Time.deltaTime * 1);
                await UniTask.NextFrame(_hpGaugeToken.Token);
            }
        }


        _isChangeDownHP = false;
        _isChangeUpHP = false;

    }


}
