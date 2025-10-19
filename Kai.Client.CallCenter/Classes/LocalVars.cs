
    //// Check
    //public static bool IsEnded(int first, int second)
    //{
    //    return first == c_nRepeatShort && second == c_nRepeatShort;
    //}

    //// Registry
    //public static bool LoadRegistry() // 임시로...
    //{
    //    s_sKaiLogId = s_KaiReg.GetStringValue("Kaitech_sId");
    //    if (string.IsNullOrEmpty(s_sKaiLogId)) return false;

    //    s_sKaiLogPw = s_KaiReg.GetStringValue("Kaitech_sPw");
    //    if (string.IsNullOrEmpty(s_sKaiLogPw)) return false;

    //    //string autoReceipt = s_KaiReg.GetStringValue("Kaitech_bAutoReceipt");
    //    //if (string.IsNullOrEmpty(autoReceipt)) return false;
    //    //s_bAutoReceipt = StdConvert.StringToBool(autoReceipt);

    //    //string autoAlloc = s_KaiReg.GetStringValue("Kaitech_bAutoAlloc");
    //    //if (string.IsNullOrEmpty(autoAlloc)) return false;
    //    //s_bAutoAlloc = StdConvert.StringToBool(autoAlloc);

    //    return true;
    //}
    //public static void SaveRegistry()
    //{
    //    MsgBox("코딩해야 합니다.", "LocalCommon_Vars/SaveRegistry_01");
    //}

    //// ComboBox
    //public static int GetComboBoxSelectedIndex(ComboBox comboBox)
    //{
    //    return Application.Current.Dispatcher.Invoke(() =>
    //    {
    //        return comboBox.SelectedIndex;
    //    });
    //}
    //public static int GetComboBoxItemIndex(ComboBox comboBox, string targetValue)
    //{
    //    return Application.Current.Dispatcher.Invoke(() =>
    //    {
    //        if (comboBox == null || targetValue == null)
    //            return -1;

    //        for (int i = 0; i < comboBox.Items.Count; i++)
    //        {
    //            object item = comboBox.Items[i];

    //            string value = item switch
    //            {
    //                ComboBoxItem cbi => cbi.Content?.ToString(),
    //                string str => str,
    //                _ => item?.ToString()
    //            };

    //            if (value == targetValue)
    //                return i;
    //        }

    //        return -1;
    //    });
    //}
    //public static string GetSelectedComboBoxContent(ComboBox comboBox)
    //{
    //    return Application.Current.Dispatcher.Invoke(() =>
    //    {
    //        if (comboBox.SelectedItem is ComboBoxItem selectedItem)
    //        {
    //            return selectedItem.Content?.ToString();
    //        }

    //        return "";
    //    });
    //}
    //public static int SetComboBoxItemByContent(ComboBox comboBox, string content)
    //{
    //    return Application.Current.Dispatcher.Invoke(() =>
    //    {
    //        int index = GetComboBoxItemIndex(comboBox, content);
    //        if (index >= 0)
    //        {
    //            comboBox.SelectedIndex = index;
    //        }

    //        return index;
    //    });
    //}

    //// Buttoon
    //public static void ButtonEnable(Button btn, bool bEanable)
    //{
    //    if (bEanable) btn.Opacity = (double)Application.Current.FindResource("AppOpacity_Enabled");
    //    else btn.Opacity = (double)Application.Current.FindResource("AppOpacity_Disabled");
    //}


