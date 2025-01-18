using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using db;
using UnityEngine.Networking;
using System.Text;
using MEC;

/*===========================================================================================
*
*   - Json -> Dictionary Type 변환 클래스
*   - Generic 형으로 Key, Value를 받아 타입 설정
*   - LoadTable()로 배열로 파싱, ToDictionary를 통해 원하는 Key, Value를 할당
* 
===========================================================================================*/

public class TableBaseData<Key, Value>
{
    private Dictionary<Key, Value> _dic = new Dictionary<Key, Value>();

    /// <summary>
    /// Data 파싱
    /// </summary>
    public IEnumerable<Value> LoadTable(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<IEnumerable<Value>>(json);
        }
        catch (Exception)
        {
            DEBUG.LOG($"MasterDataManager : json 파싱에 실패했습니다. Error to : {json}", eColorManager.UI);
        }

        return null;
    }

    /// <summary>
    /// Data 딕셔너리 캐싱 및 키값 설정
    /// </summary>
    public void SetDictionary(Dictionary<Key, Value> dic)
    {
        if (dic == null)
        {
            return;
        }

        _dic = new Dictionary<Key, Value>(dic);
    }

    /// <summary>
    /// 딕셔너리 전체 반환
    /// </summary>
    public Dictionary<Key, Value> GetDic() => _dic;

    /// <summary>
    /// 별도 데이터 반환
    /// </summary>
    public Value GetData(Key key)
    {
        return _dic.TryGetValue(key, out Value value) ? (Value)Convert.ChangeType(value, typeof(Value)) : default;
    }
}

/*===========================================================================================
*
*   - 앱 실행에 필요한 모든 DB 데이터 캐싱
* 
===========================================================================================*/

/// <summary>
/// 변수 선언용 partial class
/// </summary>
public partial class MasterDataManager : Singleton<MasterDataManager>
{
    #region TableBaseData
    #region 아바타
    public TableBaseData<(int, int), AvatarPreset> dataAvatarPreset { get; private set; }
    public TableBaseData<int, AvatarResetInfo> dataAvatarResetInfo { get; private set; }
    public TableBaseData<(int, int), KtmfSpecialItem> dataKtmfSpecialItem { get; private set; }
    public TableBaseData<int, ItemUseEffect> dataItemUseEffect { get; private set; }
    public TableBaseData<(int, int), ItemMaterial> dataItemMaterial { get; private set; }
    #endregion

    #region 마이룸
    public TableBaseData<int, CategoryType> dataCategoryType { get; private set; }
    public TableBaseData<int, GradeType> dataGradeType { get; private set; }
    public TableBaseData<int, InteriorInstallInfo> dataInteriorInstallInfo { get; private set; }
    public TableBaseData<int, Item> dataItem { get; private set; }
    public TableBaseData<int, ItemType> dataItemType { get; private set; }
    public TableBaseData<int, LayerType> dataLayerType { get; private set; }
    public TableBaseData<int, PackageType> dataPackageType { get; private set; }
    public TableBaseData<int, StoreType> dataStoreType { get; private set; }
    #endregion

    #region 공간 정보
    public TableBaseData<int, CommerceZoneItem> dataCommerceZoneItem { get; private set; }
    public TableBaseData<int, CommerceZoneMannequin> dataCommerceZoneMannequin { get; private set; }
    public TableBaseData<int, MapExposulInfo> dataMapExposulInfo { get; private set; }
    public TableBaseData<(int, string), MapExposulBrand> dataMapExposulBrand { get; private set; }
    #endregion

    #region 사용자 제공 정보 및 세팅
    public TableBaseData<string, Localization> dataLocalization { get; private set; }
    public TableBaseData<int, BusinessCardTemplate> dataBusinessCardTemplate { get; private set; }
    public TableBaseData<int, Faq> dataFaq { get; private set; }
    public TableBaseData<int, FunctionTable> dataFunctionTable { get; private set; }
    public TableBaseData<int, CountryCode> dataCountryCode { get; private set; }
    public TableBaseData<int, ForbiddenWords> dataForbiddenWords { get; private set; }
    #endregion

    #region 게임
    public TableBaseData<int, JumpingMatchingLevel> dataJumpingMatchingLevel { get; private set; }
    public TableBaseData<int, QuizLevel> dataQuizLevel { get; private set; }
    public TableBaseData<int, QuizQuestionAnswer> dataQuizQuestionAnswer { get; private set; }
    public TableBaseData<int, QuizRoundTime> dataQuizRoundTime { get; private set; }
    #endregion

    #region NPC
    public TableBaseData<(int, int), NpcCostume> dataNpcCostume { get; private set; }
    public TableBaseData<int, NpcList> dataNpcList { get; private set; }
    #endregion

    #region 회의실
    public TableBaseData<(int, int), db.OfficeAuthority> dataOfficeAuthority { get; private set; }
    public TableBaseData<int, OfficeBookmark> dataOfficeBookmark { get; private set; }
    public TableBaseData<int, OfficeDefaultOption> dataOfficeDefaultOption { get; private set; }
    public TableBaseData<int, OfficeMode> dataOfficeMode { get; private set; }
    public TableBaseData<int, OfficeGradeAuthority> dataOfficeGradeAuthority { get; private set; }
    public TableBaseData<(int, int), OfficeModeSlot> dataOfficeModeSlot { get; private set; }
    public TableBaseData<int, OfficeShowRoomInfo> dataOfficeShowRoomInfo { get; private set; }
    public TableBaseData<int, OfficeSpaceInfo> dataOfficeSpaceInfo { get; private set; }
    public TableBaseData<(int, int), OfficeExposure> dataOfficeExposure { get; private set; }
    public TableBaseData<int, OfficeProductItem> dataOfficeProductItem { get; private set; }
    public TableBaseData<(int, int), OfficeSeatInfo> dataOfficeSeatInfo { get; private set; }
    public TableBaseData<int, db.BannerInfo> bannerInfo { get; private set; }
    #endregion

    #region Postbox
    public TableBaseData<(int, int), PostalItemProperty> dataPostalItemProperty { get; private set; }
    public TableBaseData<int, PostalMoneyProperty> dataPostalMoneyProperty { get; private set; }
    #endregion
    #endregion
}

public partial class MasterDataManager : Singleton<MasterDataManager>
{
    public string forceStorageUrl;
    public bool masterDataIsDone { get; private set; } = false;

    private readonly string _folderName = "master";
    private readonly string _fileName = "master.json";

    public void Initialize()
    {
        InitMasterData();
    }

    /// <summary>
    /// MasterData 초기화
    /// </summary>
    private void InitMasterData()
    {
        string masterUrl = Path.Combine(forceStorageUrl ?? Single.Web.StorageUrl, _folderName, _fileName);

        Single.Web.SendRequest<string>(new SendRequestData(eHttpVerb.GET, masterUrl), (result) => { LoadMasterData(result); });
    }

    /// <summary>
    /// MasterData 캐싱
    /// </summary>
    private void LoadMasterData(string resultStr)
    {
        if (string.IsNullOrEmpty(resultStr))
        {
            DEBUG.LOGERROR("MasterDataManager : LoadMasterData에 실패하였습니다.", eColorManager.DATA);
            return;
        }

        var jobj = (JObject)JsonConvert.DeserializeObject(resultStr);
        foreach (var x in jobj)
        {
            switch (x.Key)
            {
                #region 아바타
                case "AvatarPreset":
                    {
                        dataAvatarPreset = new TableBaseData<(int, int), AvatarPreset>();
                        dataAvatarPreset.SetDictionary(dataAvatarPreset.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.presetType, x.partsType)));
                    }
                    break;
                case "AvatarResetInfo":
                    {
                        dataAvatarResetInfo = new TableBaseData<int, AvatarResetInfo>();
                        dataAvatarResetInfo.SetDictionary(dataAvatarResetInfo.LoadTable(x.Value.ToString())?.ToDictionary(x => x.itemId));
                    }
                    break;
                case "ItemUseEffect":
                    {
                        dataItemUseEffect = new TableBaseData<int, ItemUseEffect>();
                        dataItemUseEffect.SetDictionary(dataItemUseEffect.LoadTable(x.Value.ToString())?.ToDictionary(x => x.itemId));
                    }
                    break;
                case "KtmfSpecialItem":
                    {
                        dataKtmfSpecialItem = new TableBaseData<(int, int), KtmfSpecialItem>();
                        dataKtmfSpecialItem.SetDictionary(dataKtmfSpecialItem.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.costumeId, x.partsId)));
                    }
                    break;
                case "ItemMaterial":
                    {
                        dataItemMaterial = new TableBaseData<(int, int), ItemMaterial>();
                        dataItemMaterial.SetDictionary(dataItemMaterial.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.itemId, x.num)));
                    }
                    break;
                #endregion

                #region 마이룸
                case "InteriorInstallInfo":
                    {
                        dataInteriorInstallInfo = new TableBaseData<int, InteriorInstallInfo>();
                        dataInteriorInstallInfo.SetDictionary(dataInteriorInstallInfo.LoadTable(x.Value.ToString())?.ToDictionary(x => x.itemId));
                    }
                    break;
                case "Item":
                    {
                        dataItem = new TableBaseData<int, Item>();
                        dataItem.SetDictionary(dataItem.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "Localization":
                    {
                        dataLocalization = new TableBaseData<string, Localization>();
                        dataLocalization.SetDictionary(dataLocalization.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                #endregion

                #region 공간 정보
                case "CommerceZoneItem":
                    {
                        dataCommerceZoneItem = new TableBaseData<int, CommerceZoneItem>();
                        dataCommerceZoneItem.SetDictionary(dataCommerceZoneItem.LoadTable(x.Value.ToString())?.ToDictionary(x => x.itemId));
                    }
                    break;
                case "CommerceZoneMannequin":
                    {
                        dataCommerceZoneMannequin = new TableBaseData<int, CommerceZoneMannequin>();
                        dataCommerceZoneMannequin.SetDictionary(dataCommerceZoneMannequin.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "MapExposulInfo":
                    {
                        dataMapExposulInfo = new TableBaseData<int, MapExposulInfo>();
                        dataMapExposulInfo.SetDictionary(dataMapExposulInfo.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "MapExposulBrand":
                    {
                        dataMapExposulBrand = new TableBaseData<(int, string), MapExposulBrand>();
                        dataMapExposulBrand.SetDictionary(dataMapExposulBrand.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.mapExposulInfoId, x.brandName)));
                    }
                    break;
                #endregion

                #region 사용자 제공 정보 및 세팅
                case "BusinessCardTemplate":
                    {
                        dataBusinessCardTemplate = new TableBaseData<int, BusinessCardTemplate>();
                        dataBusinessCardTemplate.SetDictionary(dataBusinessCardTemplate.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "CountryCode":
                    {
                        dataCountryCode = new TableBaseData<int, CountryCode>();
                        dataCountryCode.SetDictionary(dataCountryCode.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "Faq":
                    {
                        dataFaq = new TableBaseData<int, Faq>();
                        dataFaq.SetDictionary(dataFaq.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "ForbiddenWords":
                    {
                        dataForbiddenWords = new TableBaseData<int, ForbiddenWords>();
                        dataForbiddenWords.SetDictionary(dataForbiddenWords.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "FunctionTable":
                    {
                        dataFunctionTable = new TableBaseData<int, FunctionTable>();
                        dataFunctionTable.SetDictionary(dataFunctionTable.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                #endregion

                #region 게임
                case "JumpingMatchingLevel":
                    {
                        dataJumpingMatchingLevel = new TableBaseData<int, JumpingMatchingLevel>();
                        dataJumpingMatchingLevel.SetDictionary(dataJumpingMatchingLevel.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "QuizLevel":
                    {
                        dataQuizLevel = new TableBaseData<int, QuizLevel>();
                        dataQuizLevel.SetDictionary(dataQuizLevel.LoadTable(x.Value.ToString())?.ToDictionary(x => x.level));
                    }
                    break;
                case "QuizQuestionAnswer":
                    {
                        dataQuizQuestionAnswer = new TableBaseData<int, QuizQuestionAnswer>();
                        dataQuizQuestionAnswer.SetDictionary(dataQuizQuestionAnswer.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "QuizRoundTime":
                    {
                        dataQuizRoundTime = new TableBaseData<int, QuizRoundTime>();
                        dataQuizRoundTime.SetDictionary(dataQuizRoundTime.LoadTable(x.Value.ToString())?.ToDictionary(x => x.round));
                    }
                    break;
                #endregion

                #region NPC
                case "NpcCostume":
                    {
                        dataNpcCostume = new TableBaseData<(int, int), NpcCostume>();
                        dataNpcCostume.SetDictionary(dataNpcCostume.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.npcId, x.partsType)));
                    }
                    break;
                case "NpcList":
                    {
                        dataNpcList = new TableBaseData<int, NpcList>();
                        dataNpcList.SetDictionary(dataNpcList.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                #endregion

                #region 회의실
                case "OfficeAuthority":
                    {
                        dataOfficeAuthority = new TableBaseData<(int, int), db.OfficeAuthority>();
                        dataOfficeAuthority.SetDictionary(dataOfficeAuthority.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.modeType, x.permissionType)));
                    }
                    break;
                case "OfficeBookmark":
                    {
                        dataOfficeBookmark = new TableBaseData<int, OfficeBookmark>();
                        dataOfficeBookmark.SetDictionary(dataOfficeBookmark.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "OfficeDefaultOption":
                    {
                        dataOfficeDefaultOption = new TableBaseData<int, OfficeDefaultOption>();
                        dataOfficeDefaultOption.SetDictionary(dataOfficeDefaultOption.LoadTable(x.Value.ToString())?.ToDictionary(x => x.permissionType));
                    }
                    break;
                case "OfficeMode":
                    {
                        dataOfficeMode = new TableBaseData<int, OfficeMode>();
                        dataOfficeMode.SetDictionary(dataOfficeMode.LoadTable(x.Value.ToString())?.ToDictionary(x => x.modeType));
                    }
                    break;
                case "OfficeGradeAuthority":
                    {
                        dataOfficeGradeAuthority = new TableBaseData<int, OfficeGradeAuthority>();
                        dataOfficeGradeAuthority.SetDictionary(dataOfficeGradeAuthority.LoadTable(x.Value.ToString())?.ToDictionary(x => x.gradeType));
                    }
                    break;
                case "OfficeModeSlot":
                    {
                        dataOfficeModeSlot = new TableBaseData<(int, int), OfficeModeSlot>();
                        dataOfficeModeSlot.SetDictionary(dataOfficeModeSlot.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.modeType, x.permissionType)));
                    }
                    break;
                case "OfficeSpaceInfo":
                    {
                        dataOfficeSpaceInfo = new TableBaseData<int, OfficeSpaceInfo>();
                        dataOfficeSpaceInfo.SetDictionary(dataOfficeSpaceInfo.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "OfficeExposure":
                    {
                        dataOfficeExposure = new TableBaseData<(int, int), OfficeExposure>();
                        dataOfficeExposure.SetDictionary(dataOfficeExposure.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.exposureType, x.modeType)));
                    }
                    break;
                case "OfficeProductItem":
                    {
                        dataOfficeProductItem = new TableBaseData<int, OfficeProductItem>();
                        dataOfficeProductItem.SetDictionary(dataOfficeProductItem.LoadTable(x.Value.ToString())?.ToDictionary(x => x.productId));
                    }
                    break;
                case "OfficeShowRoomInfo":
                    {
                        dataOfficeShowRoomInfo = new TableBaseData<int, OfficeShowRoomInfo>();
                        dataOfficeShowRoomInfo.SetDictionary(dataOfficeShowRoomInfo.LoadTable(x.Value.ToString())?.ToDictionary(x => x.id));
                    }
                    break;
                case "OfficeSeatInfo":
                    {
                        dataOfficeSeatInfo = new TableBaseData<(int, int), OfficeSeatInfo>();
                        dataOfficeSeatInfo.SetDictionary(dataOfficeSeatInfo.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.spaceId, x.num)));
                    }
                    break;
                #endregion

                #region Postbox
                case "PostalItemProperty":
                    {
                        dataPostalItemProperty = new TableBaseData<(int, int), PostalItemProperty>();
                        dataPostalItemProperty.SetDictionary(dataPostalItemProperty.LoadTable(x.Value.ToString())?.ToDictionary(x => (x.itemType, x.categoryType)));
                    }
                    break;
                case "PostalMoneyProperty":
                    {
                        dataPostalMoneyProperty = new TableBaseData<int, PostalMoneyProperty>();
                        dataPostalMoneyProperty.SetDictionary(dataPostalMoneyProperty.LoadTable(x.Value.ToString())?.ToDictionary(x => x.moneyType));
                    }
                    break;
                    #endregion
            }
        }

        masterDataIsDone = true;

        SetDataInit();
    }

    /// <summary>
    /// 마스터 데이터 파싱 완료 시 데이터 세팅
    /// </summary>
    private void SetDataInit()
    {
        Single.ItemData.LoadResourcesData();
    }
}
