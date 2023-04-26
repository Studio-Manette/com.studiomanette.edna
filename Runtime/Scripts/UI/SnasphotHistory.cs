using UnityEngine;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class SnasphotHistory : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI NumberLabel;
        public TMPro.TextMeshProUGUI DateLabel;
        public Transform dirtyIcon;

        public string filePath { get { return _filePath; } }
        private string _filePath;

        public void Init(string _filepath, int _number)
        {
            _filePath = _filepath;
            NumberLabel.text = "#" + _number.ToString("00");
            DateLabel.text = System.DateTime.Now.ToString("hh:mm.ss");
            MarkAsDirty(false);
        }

        public void OnClickLoad()
        {
            LoadPreset();
        }

        public void OnClickDelete()
        {
            Utils.Confirm("do you really want to delete this profile ?", Destroy);
        }

        public void MarkAsDirty(bool isDirty)
        {
            dirtyIcon.gameObject.SetActive(isDirty);
        }

        private void LoadPreset()
        {
            string newOverride = CommonUtils.GetPathNameWithoutExtension(_filePath) + "_override_" + System.DateTime.Now.ToString("hhmmss") + ".mbx";
            System.IO.File.Copy(_filePath, newOverride);
            Utils.GetManager().OnLoadSnapshot(newOverride, "Restore Snapshot " + NumberLabel.text + "...");
        }

        private void Destroy()
        {
            DestroyImmediate(this.gameObject);
        }
    }
}
