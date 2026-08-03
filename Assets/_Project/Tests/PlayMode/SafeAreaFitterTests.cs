using System.Collections;
using NUnit.Framework;
using OneStep.Platform;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneStep.Tests.PlayMode
{
    public sealed class SafeAreaFitterTests
    {
        [UnityTest]
        public IEnumerator Apply_KeepsAnchorsNormalized()
        {
            var gameObject = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            yield return null;
            var rect = gameObject.GetComponent<RectTransform>();
            Assert.That(rect.anchorMin.x, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMin.y, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMax.x, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMax.y, Is.InRange(0f, 1f));
            Object.Destroy(gameObject);
        }
    }
}
