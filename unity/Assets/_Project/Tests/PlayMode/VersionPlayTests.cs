using System.Collections;
using AtelierVerse.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AtelierVerse.Tests
{
    /// <summary>
    /// 메뉴의 버전 표시를 확인한다. 값은 프로젝트 설정의 bundleVersion이다.
    /// </summary>
    public class VersionPlayTests : PlayTestBase
    {
        [UnityTest]
        public IEnumerator 메뉴에_서비스_이름과_버전이_보인다()
        {
            yield return LoadSandbox();

            TMP_Text brand = Find<TMP_Text>(ui.Menu, "Brand");

            Assert.IsFalse(string.IsNullOrEmpty(Application.version), "프로젝트 설정에 버전이 없습니다.");
            StringAssert.StartsWith(GameUi.BrandName, brand.text);
            StringAssert.Contains($"v{Application.version}", brand.text);
        }
    }
}
