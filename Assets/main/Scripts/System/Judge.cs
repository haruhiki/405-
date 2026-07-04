using UnityEngine;

public class Judge : MonoBehaviour
{
    [Header("�ݒ�")]
    [SerializeField] private int myLane;                     // 0:��, 1:�E
    [SerializeField] private float perfectWindow = 0.05f;    // �ǂ̋��e���ԍ� (�b)
    [SerializeField] private float greatWindow = 0.12f;      // �̋��e���ԍ� (�b)
    [SerializeField] private float badWindow = 0.20f;        // �s�i����ȏ㗣��Ă����疳���j�̋��e���ԍ�
    [SerializeField] private float misstakeDamage = 10.0f;   // �~�X���Ɏ󂯂�_���[�W��

    [Header("�Q��")]
    [SerializeField] private Define _defineSO;
    [SerializeField] private Charactor _charactor;

    private bool isLongPress = false;
    private NotesCon currentLongNote = null;

    void Update()
    {
        if (_defineSO == null) return;

        // �����O�m�[�c���������ςȂ��Ŋ������ANotesCon�����������ŁiDestroy�j�����ꍇ�̌��m
        if (isLongPress && currentLongNote == null)
        {
            Debug.Log("<color=orange>�y�����O�����z�m�[�c�̎������ł����m�B�t���O�𐳏탊�Z�b�g���܂��B</color>");
            isLongPress = false;
            StopLongPressSE();
            ConsumeInput();
        }

        // ���������S�̂ŉ��̓��͂��Ȃ���Α��X���[
        if (!_defineSO.HasInput) return;

        // myLane�i0:��, 1:�E�j�ɉ����Ď����̃��[���̓��͂�����o��
        bool isMyLaneKey = (myLane == 0) ? _defineSO.isLeftKey : _defineSO.isRightKey;
        if (!isMyLaneKey) return;

        // ���ʂ̓��͏�Ԃ��擾
        bool isDetected = _defineSO.isInputDetected;
        bool isHold = _defineSO.isInputHold;
        bool isRush = _defineSO.isInputRush;

        // �Ώۃm�[�c�����Ԏ����琳�m�ɃL���b�`
        NotesCon targetNote = isLongPress ? currentLongNote : GetNearestNote();
        
        // �@���ׂ��m�[�c�������V�[���ɂȂ��inull�j�̂ɃL�[���͂����c���Ă���ꍇ�̈��S��
        if (targetNote == null)
        {
            ConsumeInput();
            return;
        }

        // ���Q�[�̐�ΐ��`�F���ԍ��̌v�Z
        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;
        float timeDiff = Mathf.Abs(targetNote.GetTargetTime() - currentTime);

        // �m�[�c�^�C�v���Ƃ̔��蕪��
        switch (targetNote.GetNoteType())
        {
            case NoteDate.NotesType.Short:
                if (isDetected)
                {
                    ProcessShortHit(targetNote, timeDiff);
                }
                break;

            case NoteDate.NotesType.Long_Start:
                // �����n��(isDetected)�����łȂ��A�r������̒�����(isHold)�������ɓ����ď������邺�I
                ProcessLongHit(targetNote, timeDiff, isDetected, isHold, isRush, currentTime);
                break;

            case NoteDate.NotesType.Rush:
                if (isDetected)
                {
                    ProcessRushHit(targetNote, timeDiff);
                }
                break;
        }
    }

    /// <summary>
    /// ? ���@���ׂ��A�ł����ݎ��Ԃɋ߂��A�܂��́u���ݒʉߒ��v�̃m�[�c��1�������G
    /// </summary>
    NotesCon GetNearestNote()
    {
        NotesCon[] notes = FindObjectsByType<NotesCon>(FindObjectsSortMode.None);
        
        NotesCon best = null;
        float minDiff = badWindow; 
        float currentTime = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTime() : 0f;

        foreach (var n in notes)
        {
            if (n.GetLane() != myLane) continue;

            // ����NotesCon���ŏ��Ńt���O�������Ă���]���r�̓X���[
            //�i��NotesCon�� public bool IsDestroyed �̃v���p�e�B������ΘA���A�Ȃ���΂��̍s���R�����g�A�E�g�ł�OK�j
            // if (n.IsDestroyed) continue; 

            // ? �y�r�����画����Ƃ邽�߂̒���p���[�A�b�v�z
            // ���������O�m�[�c�ŁA���Ɏn�_���߂��Ă���icurrentTime >= targetTime�j���A
            // �܂��I�_���߂��Ă��Ȃ��icurrentTime <= endTime�j�ʉߒ��̃m�[�c�������ꍇ�A�ŗD��Ń��b�N�I������I
            if (n.GetNoteType() == NoteDate.NotesType.Long_Start && 
                currentTime >= n.GetTargetTime() && currentTime <= n.GetEndTime())
            {
                return n; // �ʉߒ��̃����O�m�[�c�𔭌������瑦���ɂ����Ԃ��I
            }

            // �ʏ�̋����v�Z�i�V���[�g��A�܂����胉�C���ɓ��B���Ă��Ȃ��m�[�c�p�j
            float diff = Mathf.Abs(n.GetTargetTime() - currentTime);
            if (diff < minDiff)
            {
                minDiff = diff;
                best = n;
            }
        }
        return best;
    }

    /// <summary> �Z�������� </summary>
    void ProcessShortHit(NotesCon note, float timeDiff)
    {
        if (timeDiff <= greatWindow)
        {
            MoveCharacterToNotePosition(transform.position);
            JudgePass(note, timeDiff);
        }
        else
        {
            HandleMissProcessing(note, timeDiff);
        }
        ConsumeInput();
    }

    /// <summary> ����������i�r������̊��荞�ݑΉ��Łj </summary>
    void ProcessLongHit(NotesCon note, float timeDiff, bool isDetected, bool isHold, bool isRush, float currentTime)
    {
        // ���� �y�V�K�����z�����O�m�[�c�́u�r������̔���擾�v���� ����
        // �܂���������ԂɂȂ��Ă��Ȃ����A�u���łɎn�_��ʉߒ��v���u�w���z�[���h(isHold)�v���ꂽ�ꍇ�I
        if (!isLongPress && isHold && currentTime >= note.GetTargetTime() && currentTime <= note.GetEndTime())
        {
            isLongPress = true;
            currentLongNote = note;
            note.SetHoldVisual(true); // �m�[�c�ɒ��������ł��邱�Ƃ�`����i����őт��k�ݏo�����I�j
            MoveCharacterToNotePosition(transform.position);

            _defineSO.PlayNoteSE(NoteDate.NotesType.Long_Start, true);
            Debug.Log("<color=lime>�y�������r�����A�z�m�[�c�̓r������z�[���h�����m�E���A�������I</color>");
            
            ConsumeInput();
            return;
        }

        // �@ �ʏ�̒������J�n�i�����ɓ�����@�����u�ԁj
        if (isDetected && !isLongPress)
        {
            if (timeDiff <= greatWindow)
            {
                isLongPress = true;
                currentLongNote = note;
                note.SetHoldVisual(true);  
                MoveCharacterToNotePosition(transform.position);

                _defineSO.PlayNoteSE(NoteDate.NotesType.Long_Start, true);
                Debug.Log("<color=cyan>�y�������J�n�z�W���X�g�^�C�~���O�Ńz�[���h�����I</color>");
            }
            else
            {
                HandleMissProcessing(note, timeDiff);
            }
            ConsumeInput();
            return;
        }

        // ����ȍ~�͒��������b�N�I���iisLongPress�j���̏���
        if (!isLongPress) return;

        // �A �r���Ŏw�����S�ɗ���Ă��܂����ꍇ�i�z�[���h���s�E���E�j
        // ���m�[�c���O���ŏ���ɏ������ꍇ�iUpdate���̎����j�������ň��S�ɊO��
        if ((!isDetected && !isHold && !isRush) || note == null)
        {
            StopLongPressSE();
            Debug.Log("<color=red>�y���������s�z�z�[���h���Ɏw�����ꂽ���I</color>");
            
            if (note != null)
            {
                note.SetHoldVisual(false);
                HandleMissProcessing(note, timeDiff);
            }
            
            isLongPress = false;
            currentLongNote = null;
            ForceResetAllInputFlags(); 
            return;
        }

        // �B �������̊����i�^�C�~���O�悭�w�𗣂����u�ԁj
        if (isRush && isLongPress)
        {
            StopLongPressSE();
            float endTimeDiff = Mathf.Abs(note.GetEndTime() - currentTime);

            note.SetHoldVisual(false);

            if (endTimeDiff <= greatWindow)
            {
                JudgePass(note, endTimeDiff);
            }
            else
            {
                HandleMissProcessing(note, endTimeDiff);
            }

            isLongPress = false;
            currentLongNote = null;
            ConsumeInput();
        }
    }

    /// <summary> �A�Ŕ��� </summary>
    void ProcessRushHit(NotesCon note, float timeDiff)
    {
        JudgePass(note, timeDiff);
        ConsumeInput();
    }

    private void JudgePass(NotesCon targetNote, float caluculateTimeDiff) 
    {
        if (targetNote == null) return;

        if (caluculateTimeDiff <= perfectWindow)
        {
            Debug.Log($"<color=orange>���� �� (Perfect) ����</color> �덷: {caluculateTimeDiff:F3}s");
            _defineSO.PlayNoteSE(targetNote.GetNoteType(), true);
            targetNote.OnHit(); 
        }
        else 
        {
            Debug.Log($"<color=yellow>�� (Great) </color> �덷: {caluculateTimeDiff:F3}s");
            _defineSO.PlayNoteSE(targetNote.GetNoteType(), true);
            targetNote.OnHit();
        }
    }

    private void HandleMissProcessing(NotesCon targetNote, float timeDiff)
    {
        Debug.Log($"<color=red>?�s�� (Miss)?</color> �덷: {timeDiff:F3}s");
        _defineSO.PlayNoteSE(targetNote.GetNoteType(), false);
        targetNote.OnMiss();

        if (_defineSO.charactorSO != null)
        {
            _defineSO.charactorSO.HPfluctuation(misstakeDamage);
        }

        if (GameSystem1.Instance != null)
        {
            GameSystem1.Instance.ApplyDamageToCharacter(misstakeDamage);
        }
    }

    private void MoveCharacterToNotePosition(Vector3 position)
    {
        if (_charactor != null) _charactor.MoveToPoint(position);
    }

    private void StopLongPressSE()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopSE();
    }

    private void ConsumeInput()
    {
        if (myLane == 0) _defineSO.isLeftKey = false;
        else _defineSO.isRightKey = false;

        _defineSO.isInputDetected = false;
        _defineSO.isInputRush = false;
    }

    private void ForceResetAllInputFlags()
    {
        _defineSO.isLeftKey = false;
        _defineSO.isRightKey = false;
        _defineSO.isInputDetected = false;
        _defineSO.isInputHold = false;
        _defineSO.isInputRush = false;
    }
}