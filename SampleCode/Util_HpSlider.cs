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

    // ������ ���ۿ� ������
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

        // ������ ��ǥ��
        _hpGaugeTarget = targetPercent;

        // ȸ�� ������
        if (_isChangeUpHP)
        {
            // ȸ�� �������� ���� ���������� ������ ����ġ�� ���߰� ����
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

        // �� ������ �� ����
        _imgHpGauge_front.fillAmount = _hpGaugeTarget;

        // �� ������ �� ����
        _imgHpGauge_back.color = _colDamageColor;

        // ���� ���� ����
        if (!_isChangeDownHP)
        {
            _isChangeDownHP = true;
            _hpGaugeToken = new CancellationTokenSource();
            ChangeHpGauge(true).Forget();
        }


    }

    public void Heal(float targetPercent)
    {
        // ������ ��ǥ��
        _hpGaugeTarget = targetPercent;

        // ���� ������
        if (_isChangeDownHP)
        {
            // ���� �������� ���� ȸ������ ������ ����ġ�� ���߰� ����
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

        // �� ������ �� ����
        _imgHpGauge_back.fillAmount = _hpGaugeTarget;

        // �� ������ �� ����
        _imgHpGauge_back.color = _colHealColor;

        // ���� ���� ����
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
            // �� ������ lerp ����
            while (_imgHpGauge_back.fillAmount > _hpGaugeTarget)
            {
                _imgHpGauge_back.fillAmount = Mathf.Lerp(_imgHpGauge_back.fillAmount, _hpGaugeTarget - 0.05f, Time.deltaTime * 1);
                await UniTask.NextFrame(_hpGaugeToken.Token);
            }
        }
        else
        {
            // �� ������ lerp ����
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
