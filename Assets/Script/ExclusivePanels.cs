using System.Collections.Generic;
using UnityEngine;

public class ExclusivePanels : MonoBehaviour
{
    public List<GameObject> panels = new List<GameObject>();
    public bool closed = true;

    readonly List<GameObject> _hidden = new List<GameObject>();
    GameObject _current;

    void Update()
    {
        GameObject opened = NewlyOpened();
        if (opened != null)
        {
            if (_current != null && _current != opened)
            {
                _current.SetActive(false);
                _hidden.Add(_current);
            }
            _hidden.Remove(opened);
            _current = opened;
            closed = false;
            return;
        }

        if (_current != null && !_current.activeSelf)
            Restore();
    }

    public void Close()
    {
        if (_current != null)
            _current.SetActive(false);
        Restore();
    }

    GameObject NewlyOpened()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            GameObject panel = panels[i];
            if (panel != null && panel.activeSelf && panel != _current)
                return panel;
        }
        return null;
    }

    void Restore()
    {
        _current = null;
        while (_hidden.Count > 0)
        {
            GameObject panel = _hidden[_hidden.Count - 1];
            _hidden.RemoveAt(_hidden.Count - 1);
            if (panel == null)
                continue;
            panel.SetActive(true);
            _current = panel;
            closed = false;
            return;
        }
        closed = true;
    }
}
