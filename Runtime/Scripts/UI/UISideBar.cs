using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace StudioManette.Edna
{
    public class UISideBar : MonoBehaviour
    {
        // Start is called before the first frame update
        public float posXClosed = -250;
        public float posXOpened = 0;
        private RectTransform rt;

        void Start()
        {
            rt = this.GetComponent<RectTransform>();
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void FoldIn()
        {
            RectTransform rt = this.GetComponent<RectTransform>();
            //this.transform.DOMoveX(posXClosed, 0.5f);
            rt.DOAnchorPosX(posXClosed, 0.5f);
        }

        public void FoldOut()
        {
            //this.transform.DOMoveX(0, 0.5f);
            rt.DOAnchorPosX(posXOpened, 0.5f);
        }
    }
}
