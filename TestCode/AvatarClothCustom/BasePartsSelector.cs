/*===========================================================================================
*                                 
*   - 플레이어 아바타 의상 교체
*   - 해당 스크립트 혹은 상속 받은 스크립트를 플레이어 오브젝트에 추가하여 사용
*   
*   - 헤어, 상의, 하의, 신발, 악세서리, 한벌 옷(상의 하의 신발 범위 포함) 총 6개 구분
*   - 의상 변경 데이터 입력 -> 프리팹 교체 -> 머테리얼 교체 순 진행
* 
===========================================================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public class BasePartsSelector : MonoBehaviour
{
    #region 변수

    [SerializeField] AvatarChangeType changeType = AvatarChangeType.Resources;
    [SerializeField] bool castingShadow = false;

    [SerializeField] protected Transform target_Root;

    [SerializeField] protected Transform target_Acc;
    [SerializeField] protected Transform target_Hair;
    [SerializeField] protected Transform target_Onepiece;
    [SerializeField] protected Transform target_Top;
    [SerializeField] protected Transform target_Bottom;
    [SerializeField] protected Transform target_Shoes;
    [SerializeField] protected Transform target_Back;

    private SkinParts currentParts = new SkinParts();

    public enum AvatarChangeType
    {
        Addressable = 1,
        Resources = 2,
    }

    public enum PARTS_TYPE
    {
        hair = 1,
        top = 2,
        bottom = 3,
        onepiece = 4,
        shoes = 5,
        accessory = 6,
        back = 7,
    }

    private enum PATH_TYPE { prefab, mat }

    #endregion

    #region 초기화

    /// <summary>
    /// 루트 타겟 초기화
    /// </summary>
    public void SetTarget(Transform _target)
    {
        if (_target == null) return;

        target_Acc = Util.Search<Transform>(_target, Define.TARGET_ACC);
        target_Hair = Util.Search<Transform>(_target, Define.TARGET_HAIR);
        target_Onepiece = Util.Search<Transform>(_target, Define.TARGET_ONEPIECE);
        target_Top = Util.Search<Transform>(_target, Define.TARGET_TOP);
        target_Bottom = Util.Search<Transform>(_target, Define.TARGET_BOTTOM);
        target_Shoes = Util.Search<Transform>(_target, Define.TARGET_SHOES);
        target_Back = Util.Search<Transform>(_target, Define.TARGET_BACK);

        target_Root = _target;
    }

    #endregion

    #region SetAvatar

    /// <summary>
    /// 전체 코스튬 변경
    /// </summary>
    public void SetAvatar(SkinParts _skinParts, bool log = true)
    {
        // 현재 입은 옷 전체 제거
        ClearAllSkins();

        // 한벌 옷인지 아닌지에 따른 상,하의 신발 교체
        if (_skinParts.onepiece != null)
        {
            ChangeParts(PARTS_TYPE.onepiece, _skinParts.onepiece);
        }
        else
        {
            ChangeParts(PARTS_TYPE.top, _skinParts.top);
            ChangeParts(PARTS_TYPE.bottom, _skinParts.bottom);
            ChangeParts(PARTS_TYPE.shoes, _skinParts.shoes);
        }
        ChangeParts(PARTS_TYPE.hair, _skinParts.hair);
        ChangeParts(PARTS_TYPE.accessory, _skinParts.accessory);
        ChangeParts(PARTS_TYPE.back, _skinParts.back);

        // 변경된 의상 데이터를 기록해둠 -> 변경되기 전 의상 데이터 필요 시 사용
        if (log) LogAllParts(_skinParts);
    }

    /// <summary>
    /// 개별 파츠 변경
    /// </summary>
    public void ChangeParts(PARTS_TYPE type, string prefabName = null, List<string> materialName = null, bool log = true, bool _resetOnepiece = false)
    {
        ChangeParts(type, new SkinPart(prefabName, materialName), log, _resetOnepiece);
    }

    public void ChangeParts(PARTS_TYPE type, SkinPart skinPart, bool log = true, bool _resetOnepiece = false)
    {
        if (skinPart == null) return;

        if (log) LogSinglePart(skinPart, type);

        switch (type)
        {
            case PARTS_TYPE.top:
            case PARTS_TYPE.bottom:
            case PARTS_TYPE.shoes:
                {
                    ClearSkins(PARTS_TYPE.onepiece);
                    // 현재 캐릭터가 한벌 옷을 입었을 경우
                    // 상의, 하의, 신발 착용 시 한벌 옷이 착용 취소 되고, 이전에 입었던 개별 부위 의상이 착용됨
                    if (_resetOnepiece) ResetSuitToTopBottomShoes();
                }
                break;
            case PARTS_TYPE.onepiece:
                {
                    ClearSkinForSuit();
                }
                break;
            default: break;
        }

        ChangeModel(skinPart.prefabName, type);
        ChangeMaterial(skinPart.materialName, type);
    }

    #endregion

    #region Core

    /// <summary>
    /// Prefab 로드 (이름)
    /// </summary>
    public void ChangeModel(string _prefabName, PARTS_TYPE type)
    {
        string path = !string.IsNullOrEmpty(_prefabName) ? _prefabName : GetDefault_Prefab(type);

        if (string.IsNullOrEmpty(path)) return;

        var combinePath = CombinePath(path, type, PATH_TYPE.prefab);
        var go = Single.Resources.Load<GameObject>(combinePath);
        ChangeModel(go, type);
    }

    /// <summary>
    /// Prefab 로드 (GameObject)
    /// </summary>
    public void ChangeModel(GameObject _prefab, PARTS_TYPE type)
    {
        if (!_prefab)
        {
            Debug.Log($"아바타 프리팹이 존재하지 않습니다.");
            return;
        }

        ClearSkins(type);
        InstantiateSkinnedMeshRenderers(_prefab, GetTarget(type));
    }

    /// <summary>
    /// material 로드 (이름)
    /// </summary>
    public void ChangeMaterial(List<string> _materialName, PARTS_TYPE type)
    {
        _materialName = _materialName == null || _materialName.Count <= 0 ? GetDefault_Mat(type) : _materialName;
        var material = new List<Material>();

        foreach (var name in _materialName.Where(name => !string.IsNullOrEmpty(name)))
        {
            var combinePath = CombinePath(name, type, PATH_TYPE.mat);

            if (!string.IsNullOrEmpty(combinePath))
            {
                var mat = Single.Resources.Load<Material>(combinePath);
                if (mat != null)
                {
                    material.Add(mat);
                }
            }
        }

        ChangeMaterial(material, type);
    }

    /// <summary>
    /// material 로드 (Material)
    /// </summary>
    public void ChangeMaterial(List<Material> _material, PARTS_TYPE type)
    {
        if (_material == null || _material.Count <= 0)
        {
            Debug.Log($"머테리얼이 존재하지 않습니다.");
            return;
        }

        var renderers = GetTarget(type).GetComponentsInChildren<SkinnedMeshRenderer>();

        if (renderers == null) return;

        int length = Mathf.Min(renderers.Length, _material.Count);
        for (int i = 0; i < length; i++)
        {
            if (renderers[i].name.Contains(Define.BODY, StringComparison.OrdinalIgnoreCase)) continue;

            // 몸을 제외한 의상 오브젝트의 material을 변경
            renderers[i].materials = _material[i];
        }
    }

    /// <summary>
    /// 복제 및 Bone을 매핑한 오브젝트를 생성하여
    /// 플레이어 아바타 오브젝트 하위의 target에 붙인다
    /// </summary>
    private void InstantiateSkinnedMeshRenderers(GameObject prefab, Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("타겟이 존재하지 않아서 스킨을 바꿀 수 없습니다.");
            return;
        }

        // Prefab의 SkinnedMeshRenderer 데이터 가져오기
        // GetComponentsInChildren 사용 → 작업물에 따라 한 Prefab에 2개 이상 2개 이상의 SkinnedMeshRenderer가 존재할 수 있음
        var newMeshes = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (newMeshes == null)
        {
            Debug.LogWarning("스킨드 매쉬 렌더러가 없습니다.");
            return;
        }

        foreach (var newMesh in newMeshes)
        {
            var targetTr = new GameObject(newMesh.name); // 빈 GameObject 생성
            var targetMesh = targetTr.AddComponent<SkinnedMeshRenderer>(); // SkinnedMeshRenderer 추가

            targetTr.layer = LayerMask.NameToLayer("Player");
            targetTr.transform.SetParent(target); // target Transform의 자식으로 설정

            // SkinnedMeshRenderer의 원래 위치와 기울기로 설정
            targetTr.transform.localPosition = newMesh.transform.localPosition;
            targetTr.transform.localRotation = newMesh.transform.localRotation;

            targetMesh.shadowCastingMode = castingShadow ? ShadowCastingMode.On : ShadowCastingMode.Off; // 쉐도우 캐스팅 유무
            targetMesh.sharedMesh = newMesh.sharedMesh; // 로드한 sharedMesh를 타깃메쉬에 할당

            // 로드한 mesh의 bone과 같은 이름의 bone을 찾아서 적용
            ChangeBone(targetMesh, newMesh);

            targetMesh.localBounds = newMesh.localBounds;
            targetMesh.materials = newMesh.materials;
        }
    }

    /// <summary>
    /// 현재 로드한 mesh의 bone과 같은 이름의 bone을 찾아서 적용
    /// </summary>
    private void ChangeBone(SkinnedMeshRenderer targetMesh, SkinnedMeshRenderer newMesh)
    {
        var root = target_Root.Find("Root");
        var children = root.GetComponentsInChildren<Transform>(true);

        var bones = new Transform[newMesh.bones.Length];

        for (int i = 0; i < newMesh.bones.Length; i++)
        {
            bones[i] = children.FirstOrDefault(c => c.name == newMesh.bones[i].name) ?? newMesh.bones[i];
        }

        targetMesh.bones = bones;
        targetMesh.rootBone = children.FirstOrDefault(c => c.name == newMesh.rootBone.name);
    }
    #endregion

    #region ClearSkins
    public void ClearSkins(params PARTS_TYPE[] target)
    {
        foreach (PARTS_TYPE type in target)
        {
            var tr = GetTarget(type);

            if (tr == null) continue;

            foreach (Transform child in tr)
            {
                Destroy(child.gameObject);
            }
        }
    }

    protected void ClearAllSkins() => ClearSkins(Util.Enum2List<PARTS_TYPE>().ToArray());

    protected void ClearSkinForSuit() => ClearSkins(PARTS_TYPE.top, PARTS_TYPE.bottom, PARTS_TYPE.shoes);
    #endregion

    #region Log Parts
    // 착용한 파츠를 기록해둔다.
    // 이전 파츠 기록을 가져오기 위함

    /// <summary>
    /// 전체 파츠 내역 저장
    /// </summary>
    private void LogAllParts(SkinParts _skinParts)
    {
        currentParts = _skinParts;
    }

    /// <summary>
    /// 개별 파츠 내역 저장 (프리팹명, 마테리얼 리스트)
    /// </summary>
    private void LogSinglePart(string _prefabName, List<string> _materialName, PARTS_TYPE _avatarPartsType)
    {
        LogSinglePart(new SkinPart(_prefabName, _materialName), _avatarPartsType);
    }

    /// <summary>
    /// 개별 파츠 내역 저장 (SkinPart)
    /// </summary>
    private void LogSinglePart(SkinPart _skinPart, PARTS_TYPE _avatarPartsType)
    {
        switch (_avatarPartsType)
        {
            case PARTS_TYPE.hair: currentParts.hair = _skinPart; break;
            case PARTS_TYPE.top: currentParts.top = _skinPart; break;
            case PARTS_TYPE.bottom: currentParts.bottom = _skinPart; break;
            case PARTS_TYPE.onepiece: currentParts.onepiece = _skinPart; break;
            case PARTS_TYPE.shoes: currentParts.shoes = _skinPart; break;
            case PARTS_TYPE.accessory: currentParts.accessory = _skinPart; break;
            case PARTS_TYPE.back: currentParts.back = _skinPart; break;

            default: throw new ArgumentOutOfRangeException(nameof(_avatarPartsType), _avatarPartsType, null);
        }
    }

    #endregion

    #region 기능 메소드

    /// <summary>
    /// 파츠 부모 오브젝트 Transform 반환
    /// </summary>
    private Transform GetTarget(PARTS_TYPE partType)
    {
        Transform target = null;

        switch (partType)
        {
            case PARTS_TYPE.hair: target = target_Hair; break;
            case PARTS_TYPE.top: target = target_Top; break;
            case PARTS_TYPE.bottom: target = target_Bottom; break;
            case PARTS_TYPE.onepiece: target = target_Onepiece; break;
            case PARTS_TYPE.shoes: target = target_Shoes; break;
            case PARTS_TYPE.accessory: target = target_Acc; break;
            case PARTS_TYPE.back: target = target_Back; break;
            default: break;
        }
        return target;
    }

    /// <summary>
    /// 파츠 프리팹 디폴트 세팅 이름 반환
    /// </summary>
    private string GetDefault_Prefab(PARTS_TYPE partType)
    {
        string defalutName = null;

        switch (partType)
        {
            case PARTS_TYPE.hair: defalutName = Define.DEFALUT_PREFAB_HAIR; break;
            case PARTS_TYPE.top: defalutName = Define.DEFALUT_PREFAB_TOP; break;
            case PARTS_TYPE.bottom: defalutName = Define.DEFALUT_PREFAB_BOTTOM; break;
            case PARTS_TYPE.onepiece: defalutName = Define.DEFALUT_PREFAB_ONEPIECE; break;
            case PARTS_TYPE.shoes: defalutName = Define.DEFALUT_PREFAB_SHOES; break;
            case PARTS_TYPE.accessory: defalutName = Define.DEFALUT_PREFAB_ACC; break;
            case PARTS_TYPE.back: defalutName = Define.DEFALUT_PREFAB_BACK; break;
            default: break;
        }
        return defalutName;
    }

    /// <summary>
    /// 파츠 마테리얼 디폴트 세팅 이름 반환
    /// </summary>
    private List<string> GetDefault_Mat(PARTS_TYPE partType)
    {
        List<string> defalutName = null;

        switch (partType)
        {
            case PARTS_TYPE.hair: defalutName = Define.DEFALUT_MAT_HAIR; break;
            case PARTS_TYPE.top: defalutName = Define.DEFALUT_MAT_TOP; break;
            case PARTS_TYPE.bottom: defalutName = Define.DEFALUT_MAT_BOTTOM; break;
            case PARTS_TYPE.onepiece: defalutName = Define.DEFALUT_MAT_ONEPIECE; break;
            case PARTS_TYPE.shoes: defalutName = Define.DEFALUT_MAT_SHOES; break;
            case PARTS_TYPE.accessory: defalutName = Define.DEFALUT_MAT_ACC; break;
            case PARTS_TYPE.back: defalutName = Define.DEFALUT_MAT_BACK; break;

            //case PARTS_TYPE.body: defalutName = Define.DEFALUT_MAT_BODY; break;
            //case PARTS_TYPE.head: defalutName = Define.DEFALUT_MAT_HEAD; break;
            default: break;
        }
        return defalutName;
    }

    /// <summary>
    /// 리소스 폴더 Path 반환
    /// </summary>
    private string CombinePath(string path, PARTS_TYPE partType, PATH_TYPE pathType)
    {
        string combinePath = null;

        if (!string.IsNullOrEmpty(path)
            && changeType == AvatarChangeType.Resources)
        {
            bool isPrefab = pathType == PATH_TYPE.prefab;
            switch (partType)
            {
                case PARTS_TYPE.hair: combinePath = (isPrefab ? Define.PATH_PREFAB_HAIR : Define.PATH_MAT_HAIR) + path; break;
                case PARTS_TYPE.top: combinePath = (isPrefab ? Define.PATH_PREFAB_TOP : Define.PATH_MAT_TOP) + path; break;
                case PARTS_TYPE.bottom: combinePath = (isPrefab ? Define.PATH_PREFAB_BOTTOM : Define.PATH_MAT_BOTTOM) + path; break;
                case PARTS_TYPE.onepiece: combinePath = (isPrefab ? Define.PATH_PREFAB_ONEPIECE : Define.PATH_MAT_ONEPIECE) + path; break;
                case PARTS_TYPE.shoes: combinePath = (isPrefab ? Define.PATH_PREFAB_SHOES : Define.PATH_MAT_SHOES) + path; break;
                case PARTS_TYPE.accessory: combinePath = (isPrefab ? Define.PATH_PREFAB_ACC : Define.PATH_MAT_ACC) + path; break;
                case PARTS_TYPE.back: combinePath = (isPrefab ? Define.PATH_PREFAB_BACK : Define.PATH_MAT_BACK) + path; break;
                default: break;
            }
        }
        return combinePath;
    }

    /// <summary>
    /// 현재 저장된 각 부위의 파츠 데이터를 반환
    /// </summary>
    public SkinPart GetSkinPart(PARTS_TYPE partType)
    {
        switch (partType)
        {
            case PARTS_TYPE.hair: return currentParts.hair;
            case PARTS_TYPE.top: return currentParts.top;
            case PARTS_TYPE.bottom: return currentParts.bottom;
            case PARTS_TYPE.onepiece: return currentParts.onepiece;
            case PARTS_TYPE.shoes: return currentParts.shoes;
            case PARTS_TYPE.accessory: return currentParts.accessory;
            case PARTS_TYPE.back: return currentParts.back;

            default: break;
        }
    }

    /// <summary>
    /// 수트(코스튬) 착용 도중 상의,하의,신발 변경 시 이전에 착용했던 상의,하의,신발로 되돌린다.
    /// </summary>
    public void ResetSuitToTopBottomShoes()
    {
        ChangeParts(PARTS_TYPE.top, currentParts.top);
        ChangeParts(PARTS_TYPE.bottom, currentParts.bottom);
        ChangeParts(PARTS_TYPE.shoes, currentParts.shoes);
    }

    #endregion
}

#region 데이터 클래스

/// <summary>
/// 통합 파츠 데이터
/// hair, top, bottom, shoes만 초기화가 new SkinPart()인 이유 
/// => 아바타 리셋 시 해당 4부위의 데이터가 null이면 안 되기 때문에 초기화해준다.
/// => null이 아니어야 ChangeModel 등의 로직을 타 디폴트 데이터를 사용하기 때문이다.
/// </summary>
[Serializable]
public class SkinParts
{
    public SkinPart hair = new SkinPart();
    public SkinPart top = new SkinPart();
    public SkinPart bottom = new SkinPart();
    public SkinPart shoes = new SkinPart();
    public SkinPart accessory = null;
    public SkinPart onepiece = null;
    public SkinPart back = null;

    public SkinParts(SkinPart _hair, SkinPart _top, SkinPart _bottom, SkinPart _shoes,
        SkinPart _accessory = null, SkinPart _onepiece = null, SkinPart _back = null)
    {
        hair = _hair;
        top = _top;
        bottom = _bottom;
        shoes = _shoes;
        accessory = _accessory;
        onepiece = _onepiece;
        back = _back;
    }

    public SkinParts() { }
}

/// <summary>
/// 개별 파츠 데이터
/// </summary>
[Serializable]
public class SkinPart
{
    public string prefabName = null;
    public List<string> materialName = null;

    public SkinPart(string _prefabName, List<string> _materialName = null)
    {
        prefabName = _prefabName;
        materialName = _materialName;
    }

    public SkinPart() { }
}

#endregion