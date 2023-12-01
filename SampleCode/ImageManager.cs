using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageManager : MonoBehaviour
{
    #region 싱글톤

    static ImageManager _intance;

    public static ImageManager Instance
    {
        get
        {
            if (_intance == null)
                _intance = FindObjectOfType<ImageManager>();

            if (_intance == null)
            {
                GameObject obj = Instantiate(Resources.Load<GameObject>("@ImageManager"));
                _intance = obj.GetComponent<ImageManager>();

            }

            return _intance;
        }
        set
        {
            _intance = value;
        }
    }

    #endregion



    [SerializeField] Sprite _Icon_Gold;
    [SerializeField] Sprite _Icon_Mana;
    [SerializeField] Sprite[] _Icon_Skill;



    public readonly Color _colGreen = new Color(0.6f, 1, 0.7f);
    public readonly Color _colRed = new Color(1, 0.6f, 0.5f);
    public readonly Color _colGray20 = new Color(0.2f, 0.2f, 0.2f, 1);

    public readonly Color _colBurn = new Color(0.8f, 0.5f, 0.3f);
    public readonly Color _colFreeze = new Color(0.5f, 0.9f, 1f);
    public readonly Color _colPoison = new Color(0.7f, 0.3f, 0.9f);


    private void Awake()
    {
        // 메니저 오브젝트를 1개로 유지
        ImageManager[] objs = FindObjectsOfType<ImageManager>();
        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
            return;
        }
    }



    public Sprite GetIcon_Gold() => _Icon_Gold;
    public Sprite GetIcon_Mana() => _Icon_Mana;

    public Sprite GetIcon_Skill(int index) => _Icon_Skill[index];



}
