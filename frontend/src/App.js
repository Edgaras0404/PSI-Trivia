import React, { useState, useEffect } from 'react';
import { Users, Trophy, Clock, Play, LogIn, Plus, LogOut } from 'lucide-react';
import Login from './Login';
import './App.css';
import LiquidChrome from './LiquidChrome';
   import TextPressure from './TextPressure';



class GameConnection {
    constructor() {
        this.connection = null;
        this.listeners = new Map();
    }

    async connect(url) {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/6.0.1/signalr.min.js';
        document.head.appendChild(script);

        return new Promise((resolve) => {
            script.onload = () => {
                this.connection = new window.signalR.HubConnectionBuilder()
                    .withUrl(url)
                    .withAutomaticReconnect()
                    .build();

                this.connection.start()
                    .then(() => resolve(true))
                    .catch(err => {
                        console.error('Connection error:', err);
                        resolve(false);
                    });
            };
        });
    }

    on(event, callback) {
        if (!this.listeners.has(event)) {
            this.listeners.set(event, []);
            this.connection?.on(event, (...args) => {
                this.listeners.get(event).forEach(cb => cb(...args));
            });
        }
        this.listeners.get(event).push(callback);
    }

    off(event, callback) {
        const callbacks = this.listeners.get(event);
        if (callbacks) {
            const index = callbacks.indexOf(callback);
            if (index > -1) callbacks.splice(index, 1);
        }
    }

    async invoke(method, ...args) {
        return this.connection?.invoke(method, ...args);
    }
}

function TriviaGame({ username, onLogout }) {
    const [connection] = useState(() => new GameConnection());
    const [connected, setConnected] = useState(false);
    const [gameState, setGameState] = useState('menu');
    const [gameId, setGameId] = useState('');
    const [playerId, setPlayerId] = useState(null);
    const [players, setPlayers] = useState([]);
    const [currentQuestion, setCurrentQuestion] = useState(null);
    const [selectedAnswer, setSelectedAnswer] = useState(null);
    const [answerResult, setAnswerResult] = useState(null);
    const [leaderboard, setLeaderboard] = useState([]);
    const [timeLeft, setTimeLeft] = useState(0);
    const [showAnswer, setShowAnswer] = useState(false);
    const [showGlobalLeaderboard, setShowGlobalLeaderboard] = useState(false);
    const [globalLeaderboard, setGlobalLeaderboard] = useState([]);

    useEffect(() => {
        const initConnection = async () => {
            const success = await connection.connect('https://localhost:5001/gamehub');
            setConnected(success);
        };
        initConnection();
    }, [connection]);

    useEffect(() => {
        if (!connected) return;

        const handleGameCreated = (data) => {
            setGameId(data.gameId);
            setPlayerId(data.playerId);
            setGameState('lobby');
        };

        const handleJoinedGame = (data) => {
            setGameId(data.gameId);
            setPlayerId(data.playerId);
            setPlayers(data.players);
            setGameState('lobby');
        };

        const handlePlayerJoined = (data) => {
            setPlayers(data.players);
        };

        const handleGameStarted = () => {
            setGameState('playing');
            setShowAnswer(false);
        };

        const handleNewQuestion = (data) => {
            setCurrentQuestion(data);
            setSelectedAnswer(null);
            setAnswerResult(null);
            setShowAnswer(false);
            setTimeLeft(data.timeLimit);
        };

        const handleAnswerResult = (data) => {
            setAnswerResult(data);
        };

        const handleAnswerRevealed = (data) => {
            setShowAnswer(true);
            setLeaderboard(data.leaderboard);
        };

        const handleGameEnded = (data) => {
            setLeaderboard(data.leaderboard);
            setGameState('results');
        };

        const handleError = (message) => {
            alert(message);
        };

        connection.on('GameCreated', handleGameCreated);
        connection.on('JoinedGame', handleJoinedGame);
        connection.on('PlayerJoined', handlePlayerJoined);
        connection.on('GameStarted', handleGameStarted);
        connection.on('NewQuestion', handleNewQuestion);
        connection.on('AnswerResult', handleAnswerResult);
        connection.on('AnswerRevealed', handleAnswerRevealed);
        connection.on('GameEnded', handleGameEnded);
        connection.on('Error', handleError);

        return () => {
            connection.off('GameCreated', handleGameCreated);
            connection.off('JoinedGame', handleJoinedGame);
            connection.off('PlayerJoined', handlePlayerJoined);
            connection.off('GameStarted', handleGameStarted);
            connection.off('NewQuestion', handleNewQuestion);
            connection.off('AnswerResult', handleAnswerResult);
            connection.off('AnswerRevealed', handleAnswerRevealed);
            connection.off('GameEnded', handleGameEnded);
            connection.off('Error', handleError);
        };
    }, [connected, connection]);

    useEffect(() => {
        if (timeLeft > 0 && currentQuestion && !showAnswer) {
            const timer = setTimeout(() => setTimeLeft(timeLeft - 1), 1000);
            return () => clearTimeout(timer);
        }
    }, [timeLeft, currentQuestion, showAnswer]);

    const createGame = async () => {
        await connection.invoke('CreateGame', username, 5, 10);
    };

    const joinGame = async () => {
        if (!gameId.trim()) return;
        await connection.invoke('JoinGame', gameId.toUpperCase(), username);
    };

    const startGame = async () => {
        try {
            await connection.invoke('StartGame', gameId, null, null);
        } catch (error) {
            console.error('Error starting game:', error);
        }
    };

    const submitAnswer = async (index) => {
        if (selectedAnswer !== null) return;
        setSelectedAnswer(index);
        await connection.invoke('SubmitAnswer', gameId, playerId, index);
    };

    const nextQuestion = async () => {
        await connection.invoke('NextQuestion', gameId);
    };

    const fetchGlobalLeaderboard = async () => {
        try {
            const response = await fetch('https://localhost:5001/api/leaderboard/global?top=100');
            if (response.ok) {
                const data = await response.json();
                setGlobalLeaderboard(data);
                setShowGlobalLeaderboard(true);
            }
        } catch (error) {
            console.error('Error fetching leaderboard:', error);
        }
    };

    if (!connected) {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={true}
                    />
                </div>
                <div className="container">
                    <div className="loading">Connecting to server...</div>
                </div>
            </>
        );
    }

    if (showGlobalLeaderboard) {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={true}
                    />
                </div>
                <div className="modal-overlay" onClick={() => setShowGlobalLeaderboard(false)}>
                    <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h2>Global Leaderboard</h2>
                            <button onClick={() => setShowGlobalLeaderboard(false)} className="close-button">×</button>
                        </div>
                        <div className="modal-body">
                            {globalLeaderboard.length === 0 ? (
                                <p className="empty-leaderboard">No players yet. Be the first!</p>
                            ) : (
                                <div className="global-leaderboard">
                                    {globalLeaderboard.map((player, index) => (
                                        <div key={index} className={`leaderboard-row ${player.username === username ? 'highlight' : ''}`}>
                                            <div className="rank-badge">{player.rank}</div>
                                            <div className="player-details">
                                                <div className="player-username">{player.username}</div>
                                                <div className="player-games">
                                                    {player.gamesPlayed} games • {player.totalPoints} total points
                                                </div>
                                            </div>
                                            <div className="player-elo">{player.elo} ELO</div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </>
        );
    }

    if (gameState === 'menu') {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={true}
                    />
                </div>
                <div className="container">
                    <div className="user-header">
                        <div className="user-info">
                            <span>Playing as: <strong>{username}</strong></span>
                        </div>
                        <div className="header-buttons">
                            <button onClick={fetchGlobalLeaderboard} className="leaderboard-button">
                                <Trophy className="icon" />
                                Leaderboard
                            </button>
                            <button onClick={onLogout} className="logout-button">
                                <LogOut className="icon" />
                                Logout
                            </button>
                        </div>
                    </div>

                    <div className="card">
                        <div className="header">
                            <Trophy className="icon-large" />
                            <div style={{ position: 'relative', height: '120px', marginBottom: '20px' }}>
                                <TextPressure
                                    text="PSI TRIVIA"
                                    flex={true}
                                    alpha={false}
                                    stroke={false}
                                    width={true}
                                    weight={true}
                                    italic={true}
                                    textColor="#1a202c"
                                    strokeColor="#667eea"
                                    minFontSize={32}
                                />
                            </div>
                            <p>Test your knowledge with friends!</p>
                        </div>

                        <div className="button-group">
                            <button
                                onClick={createGame}
                                className="button button-primary"
                            >
                                <Plus className="icon" />
                                Create New Game
                            </button>

                            <div className="join-group">
                                <input
                                    type="text"
                                    placeholder="Game Code"
                                    value={gameId}
                                    onChange={(e) => setGameId(e.target.value.toUpperCase())}
                                    className="input input-small"
                                    maxLength={6}
                                />
                                <button
                                    onClick={joinGame}
                                    disabled={!gameId.trim()}
                                    className="button button-secondary"
                                >
                                    <LogIn className="icon" />
                                    Join
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </>
        );
    }

    if (gameState === 'lobby') {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={true}
                    />
                </div>
                <div className="container">
                    <div className="card">
                        <div className="header">
                            <h2>Game Lobby</h2>
                            <div className="game-code">{gameId}</div>
                        </div>

                        <div className="section">
                            <div className="section-header">
                                <Users className="icon" />
                                <h3>Players ({players.length}/5)</h3>
                            </div>
                            <div className="players-list">
                                {players.map((player) => (
                                    <div key={player.id} className="player-item">
                                        <span>{player.name}</span>
                                        {player.id === playerId && <span className="badge">You</span>}
                                    </div>
                                ))}
                            </div>
                        </div>

                        <button onClick={startGame} className="button button-success">
                            <Play className="icon" />
                            Start Game
                        </button>
                    </div>
                </div>
            </>
        );
    }

    if (gameState === 'playing' && currentQuestion) {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={true}
                    />
                </div>
                <div className="container">
                    <div className="card">
                        <div className="question-header">
                            <div className="question-info">
                                <span className="badge badge-purple">Question {currentQuestion.questionNumber}</span>
                                <span className="badge badge-blue">{currentQuestion.category}</span>
                                <span className={`badge badge-${currentQuestion.difficulty.toLowerCase()}`}>
                                    {currentQuestion.difficulty}
                                </span>
                            </div>
                            <div className="timer">
                                <Clock className="icon" />
                                <span>{showAnswer ? 'Time\'s up!' : `${timeLeft}s`}</span>
                            </div>
                        </div>

                        <h3 className="question-text">{currentQuestion.questionText}</h3>

                        <div className="options">
                            {currentQuestion.answerOptions.map((option, index) => {
                                let className = 'option';

                                if (showAnswer) {
                                    if (index === currentQuestion.correctAnswer) {
                                        className += ' option-selected';
                                    } else if (index === selectedAnswer) {
                                        className += ' option-selected';
                                    }
                                } else if (selectedAnswer === index) {
                                    className += ' option-selected';
                                }

                                return (
                                    <button
                                        key={index}
                                        onClick={() => submitAnswer(index)}
                                        disabled={selectedAnswer !== null || showAnswer}
                                        className={className}
                                    >
                                        {option}
                                    </button>
                                );
                            })}
                        </div>

                        {answerResult && (
                            <div className={`result ${answerResult.isCorrect ? 'result-correct' : 'result-incorrect'}`}>
                                {answerResult.isCorrect ? 'Correct!' : 'Incorrect!'}
                            </div>
                        )}

                        {showAnswer && (
                            <div className="answer-section">
                                <div className="leaderboard">
                                    <h4>Leaderboard</h4>
                                    {leaderboard.slice(0, 3).map((player, index) => (
                                        <div key={player.id} className="leaderboard-item">
                                            <span>{index + 1}. {player.name}</span>
                                            <span className="score">{player.score} pts</span>
                                        </div>
                                    ))}
                                </div>
                                <button onClick={nextQuestion} className="button button-primary">
                                    Next Question
                                </button>
                            </div>
                        )}
                    </div>
                </div>
            </>
        );
    }

    if (gameState === 'results') {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={true}
                    />
                </div>
                <div className="container">
                    <div className="card">
                        <div className="header">
                            <Trophy className="icon-large" />
                            <h2>Game Over!</h2>
                        </div>

                        <div className="results">
                            {leaderboard.map((player, index) => {
                                const medals = ['🥇', '🥈', '🥉'];
                                return (
                                    <div key={player.id} className={`result-item rank-${index + 1}`}>
                                        <div className="result-left">
                                            {index < 3 && <span className="medal">{medals[index]}</span>}
                                            <div>
                                                <div className="player-name">{player.name}</div>
                                                <div className="player-stats">{player.correctAnswers} correct answers</div>
                                            </div>
                                        </div>
                                        <div className="final-score">{player.score}</div>
                                    </div>
                                );
                            })}
                        </div>

                        <button
                            onClick={() => {
                                setGameState('menu');
                                setGameId('');
                                setPlayerId(null);
                                setPlayers([]);
                                setCurrentQuestion(null);
                                setLeaderboard([]);
                            }}
                            className="button button-primary"
                        >
                            Back to Menu
                        </button>
                    </div>
                </div>
            </>
        );
    }

    return null;
}

export default function App() {
    const [isLoggedIn, setIsLoggedIn] = useState(false);
    const [username, setUsername] = useState('');

    useEffect(() => {
        const token = localStorage.getItem('token');
        const savedUsername = localStorage.getItem('username');
        if (token && savedUsername) {
            setIsLoggedIn(true);
            setUsername(savedUsername);
        }
    }, []);

    const handleLoginSuccess = (user) => {
        setIsLoggedIn(true);
        setUsername(user);
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        localStorage.removeItem('username');
        setIsLoggedIn(false);
        setUsername('');
    };

    if (!isLoggedIn) {
        return <Login onLoginSuccess={handleLoginSuccess} />;
    }

    return <TriviaGame username={username} onLogout={handleLogout} />;
}