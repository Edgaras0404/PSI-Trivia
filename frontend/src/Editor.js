// ...existing code...
import React, { useState } from 'react';
import './Editor.css';
import App from './App';
import LiquidChrome from './LiquidChrome';

const QuestionCategory = {
    Science: "Science",
    History: "History",
    Sports: "Sports",
    Geography: "Geography",
    Literature: "Literature"
};

const DifficultyLevel = {
    Easy: "Easy",
    Medium: "Medium",
    Hard: "Hard"
};

const Editor = () => {
    const [idInput, setIdInput] = useState('');
    const [current, setCurrent] = useState({
        id: '',
        timeLimit: 20,
        questionText: '',
        answerOptions: ['', '', '', ''],
        correctAnswerIndex: 0,
        category: QuestionCategory.Geography,
        difficulty: DifficultyLevel.Easy,
    });
    const [message, setMessage] = useState('');

    const parseId = (val) => {
        const n = Number(val);
        return Number.isInteger(n) && n > 0 ? n : null;
    };

    const ensureFourOptions = (opts) => {
        const arr = Array.isArray(opts) ? [...opts] : [];
        while (arr.length < 4) arr.push('');
        return arr.slice(0, 4);
    };

    const API_BASE = 'https://localhost:5001/api/questions';

    const apiGetQuestion = async (id) => {
        const url = `${API_BASE}/${id}`;
        try {
            const res = await fetch(url);
            if (!res.ok) {
                const text = await res.text().catch(() => '');
                console.error(`GET ${url} failed: ${res.status} ${res.statusText}`, text);
                setMessage(text || `GET failed: ${res.status}`);
                return null;
            }
            return await res.json();
        } catch (err) {
            console.error(`GET ${url} network error:`, err);
            setMessage('Network error while getting question');
            return null;
        }
    };

    const apiDeleteQuestion = async (id) => {
        const url = `${API_BASE}/${id}`;
        try {
            const res = await fetch(url, { method: 'DELETE' });
            if (!res.ok) {
                const text = await res.text().catch(() => '');
                console.error(`DELETE ${url} failed: ${res.status} ${res.statusText}`, text);
                setMessage(text || `DELETE failed: ${res.status}`);
                return false;
            }
            return true;
        } catch (err) {
            console.error(`DELETE ${url} network error:`, err);
            setMessage('Network error while deleting question');
            return false;
        }
    };

    const apiCreateQuestion = async (payload) => {
        const url = `${API_BASE}`;
        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            if (!res.ok) {
                const text = await res.text().catch(() => '');
                console.error(`POST ${url} failed: ${res.status} ${res.statusText}`, text);
                setMessage(text || `CREATE failed: ${res.status}`);
                return null;
            }
            return await res.json();
        } catch (err) {
            console.error(`POST ${url} network error:`, err);
            setMessage('Network error while creating question');
            return null;
        }
    };

    const handleGet = async () => {
        const id = parseId(idInput);
        if (!id) { setMessage('Provide a numeric id'); return; }

        setMessage('Fetching...');
        const remote = await apiGetQuestion(id);
        if (remote) {
            setCurrent({ ...remote, answerOptions: ensureFourOptions(remote.answerOptions) });
            setMessage(`Loaded question ${remote.id}`);
        } else {
            setCurrent({
                id: '',
                timeLimit: 20,
                questionText: '',
                answerOptions: ['', '', '', ''],
                correctAnswerIndex: 0,
                category: QuestionCategory.Geography,
                difficulty: DifficultyLevel.Easy,
            });
        }
    };

    const handleNew = async () => {
        if (!current) { setMessage('No data to create'); return; }

        const providedId = parseId(idInput);
        const payload = {
            ...(providedId ? { id: providedId } : {}),
            timeLimit: Number(current.timeLimit) || 0,
            questionText: String(current.questionText || ''),
            answerOptions: ensureFourOptions(current.answerOptions).map(a => String(a || '')),
            correctAnswerIndex: Number(current.correctAnswerIndex) || 0,
            category: current.category,
            difficulty: current.difficulty,
        };

        setMessage('Creating on server...');
        const created = await apiCreateQuestion(payload);
        if (created) {
            setCurrent({ ...created, answerOptions: ensureFourOptions(created.answerOptions) });
            setIdInput(String(created.id));
            setMessage(`Created question ${created.id}`);
        }
    };

    const handleDelete = async () => {
        const id = parseId(idInput);
        if (!id) { setMessage('Provide a numeric id'); return; }

        setMessage('Deleting...');
        const ok = await apiDeleteQuestion(id);
        if (ok) {
            setCurrent({
                id: '',
                timeLimit: 20,
                questionText: '',
                answerOptions: ['', '', '', ''],
                correctAnswerIndex: 0,
                category: QuestionCategory.Geography,
                difficulty: DifficultyLevel.Easy,
            });
            setIdInput('');
            setMessage(`Deleted question ${id}`);
        }
    };

    const updateCurrentField = (field, value) => {
        setCurrent(prev => ({ ...prev, [field]: value }));
    };

    const updateOption = (index, value) => {
        setCurrent(prev => {
            const opts = ensureFourOptions(prev?.answerOptions);
            opts[index] = value;
            return { ...prev, answerOptions: opts };
        });
    };

    return (
        <>
            <div className="user-header">
                <div className="user-info">
                    Welcome, <strong>Not working</strong>
                </div>
                <div className="header-buttons">
                    <button className="navbar-button editor-button">
                        Editor
                    </button>
                    <button className="navbar-button leaderboard-button" >
                        Leaderboard
                    </button>
                    <button className="navbar-button logout-button" o>
                        Logout
                    </button>
                </div>
            </div>

            <div className="liquid-chrome-background">
                <LiquidChrome
                    baseColor={[0.3, 0.4, 0.5]}
                    speed={0.2}
                    amplitude={0.3}
                    frequencyX={2.5}
                    frequencyY={2.5}
                    interactive={false}
                />
            </div>

            <div className="editor-page">
                <div className="editor-card">
                    <h2 className="editor-title">Question Editor</h2>

                    <div className="controls-row">
                        <div className="id-group">
                            <label className="label-small">Id</label>
                            <input className="input-id" value={idInput} onChange={(e) => setIdInput(e.target.value)} />
                        </div>

                        <div className="inline-buttons">
                            <button type="button" className="btn" onClick={handleGet}>Get</button>
                            <button type="button" className="btn" onClick={handleNew}>New</button>
                            <button type="button" className="btn danger" onClick={handleDelete}>Delete</button>
                        </div>
                    </div>

                    <div className="form-field">
                        <label className="label-small">Time Limit (s)</label>
                        <input className="input-full" type="number" value={current?.timeLimit ?? ''} min={5} onChange={(e) => updateCurrentField('timeLimit', e.target.value)} />
                    </div>

                    <div className="form-field">
                        <label className="label-small">Question Text</label>
                        <textarea className="question-textarea" value={current?.questionText ?? ''} onChange={(e) => updateCurrentField('questionText', e.target.value)} rows={4} />
                    </div>

                    <div className="form-field">
                        <label className="label-small">Answer Options (always 4)</label>
                        <div className="options-list">
                            {Array.from({ length: 4 }).map((_, idx) => {
                                const opt = (current?.answerOptions && current.answerOptions[idx]) ?? '';
                                return (
                                    <div key={idx} className="option-row">
                                        <input className="input-option" value={opt} onChange={(e) => updateOption(idx, e.target.value)} />
                                        <label className="radio-label">
                                            <input type="radio" name="correct" checked={Number(current?.correctAnswerIndex) === idx} onChange={() => updateCurrentField('correctAnswerIndex', idx)} />
                                        </label>
                                    </div>
                                );
                            })}
                        </div>
                    </div>

                    <div className="two-columns">
                        <div className="form-field">
                            <label className="label-small">Category</label>
                            <select className="input-full" value={current?.category ?? QuestionCategory.Geography} onChange={(e) => updateCurrentField('category', e.target.value)}>
                                {Object.values(QuestionCategory).map(v => <option key={v} value={v}>{v}</option>)}
                            </select>
                        </div>

                        <div className="form-field">
                            <label className="label-small">Difficulty</label>
                            <select className="input-full" value={current?.difficulty ?? DifficultyLevel.Easy} onChange={(e) => updateCurrentField('difficulty', e.target.value)}>
                                {Object.values(DifficultyLevel).map(v => <option key={v} value={v}>{v}</option>)}
                            </select>
                        </div>
                    </div>

                    <div className="message-row">{message}</div>
                </div>
            </div>
        </>
    );
};

export default Editor;