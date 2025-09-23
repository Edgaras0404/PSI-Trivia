import React, { useState, useEffect, useRef } from 'react';
import './App.css';


const TriviaGame = () => {
  const [gameState, setGameState] = useState('main');
  const [playerName, setPlayerName] = useState('');
  const [currentQuestion, setCurrentQuestion] = useState(null);
  const [score, setScore] = useState(0);
  const [selectedAnswer, setSelectedAnswer] = useState('');
  const [timer, setTimer] = useState(null);
  const timerRef = useRef();
  const mockLeaderboard = [
    { name: "A", score: 0 },
    { name: "B", score: 3 }
  ];
  const [leaderboard, setLeaderboard] = useState(mockLeaderboard);

  // this will later be fetched from an API, now it's hardcoded for simplicity

  const mockQuestion = [
    {
      id: 1,
      time_s: 5,
      question: "What is the capital of France?",
      options: ["London", "Berlin", "Paris", "Madrid"],
      correctAnswer: "Paris"
    },
    {
      id: 2,
      time_s: 3,
      question: "0/1 = ?",
      options: ["0", "1", "+inf", "-inf"],
      correctAnswer: "0"
    },
    {
      id: 3,
      time_s: 10,
      question: "Which color is a mix of 2 separate wavelengths?",
      options: ["Purple", "Yellow", "Blue", "Pink"],
      correctAnswer: "Pink"
    }
  ];

  const handleStartGame = () => {
    setGameState('nickname');
  };

  const handleJoinGame = () => {
    if (!playerName.trim()) {
      alert("Please enter a valid nickname.");
      return;
    }
    setGameState('playing');
    setCurrentQuestion(mockQuestion[0]);
    setLeaderboard((prev) => [...prev, { name: playerName, score: 0 }]);
  };

  const handleAnswerSubmit = () => {
    if (selectedAnswer === currentQuestion.correctAnswer) {
      setScore(score + 1);
      setLeaderboard(
        prev => prev.map(user =>
          user.name === playerName ? { ...user, score: user.score + 1 } : user
        )
      );
    }

    if (currentQuestion.id == mockQuestion.length) {
      setGameState('finished');
      handleShowLeaderboard();
    } else {
      setCurrentQuestion(mockQuestion[currentQuestion.id]);
      setGameState('playing');
    }
  };

  const resetGame = () => {
    setGameState('main');
    setPlayerName('');
    setCurrentQuestion(null);
    setScore(0);
    setSelectedAnswer('');
  };

  const handleShowLeaderboard = () => {
    setLeaderboard((prev) => {
      const updated = prev.map(user =>
        user.name === "You" ? { ...user, score } : user
      );
      // Sort score desc
      return updated.sort((a, b) => b.score - a.score);
    });
  };

  //decrease timer
  useEffect(() => {
    if (gameState === 'playing') {
      setTimer(currentQuestion.time_s || 10);
      timerRef.current = setInterval(() => {
        setTimer((prev) => Math.max(prev - 1, 0));
      }, 1000);
    }
    return () => clearInterval(timerRef.current);
  }, [gameState, currentQuestion]);

  // When timer = 0 show leaderboard
  useEffect(() => {
    if (gameState === 'playing' && timer === 0) {
      handleAnswerSubmit();
    }
  }, [timer, gameState]);

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full relative">
        {/* Timer */}
        {gameState === 'playing' && (
          <div className="absolute top-4 right-6 bg-blue-100 text-blue-700 px-4 py-2 rounded-lg font-bold text-lg shadow">
            {timer}s
          </div>
        )}

        {/* Main Screen */}
        {gameState === 'main' && (
          <div className="text-center">
            <h1 className="text-4xl font-bold text-gray-800 mb-8">Live Trivia</h1>
            <p className="text-gray-600 mb-8">Test your knowledge in our live trivia game!</p>
            <button
              onClick={handleStartGame}
              className="w-full bg-blue-500 hover:bg-blue-600 text-white font-semibold py-4 px-8 rounded-lg transition-colors text-lg"
            >
              Start Game
            </button>
          </div>
        )}

        {/* Nickname Entry Screen */}
        {gameState === 'nickname' && (
          <div className="text-center">
            <h2 className="text-2xl font-bold text-gray-800 mb-6">Enter Your Nickname</h2>
            <input
              type="text"
              placeholder="Your nickname"
              value={playerName}
              onChange={(e) => setPlayerName(e.target.value)}
              className="w-full p-3 border border-gray-300 rounded-lg mb-6 text-center text-lg"
              maxLength={20}
              autoFocus
            />
            <button
              onClick={handleJoinGame}
              disabled={!playerName.trim()}
              className="w-full bg-green-500 hover:bg-green-600 disabled:bg-gray-300 text-white font-semibold py-3 px-6 rounded-lg transition-colors mb-4"
            >
              Join Game
            </button>
            <button
              onClick={() => setGameState('main')}
              className="w-full bg-gray-500 hover:bg-gray-600 text-white font-semibold py-2 px-6 rounded-lg transition-colors"
            >
              Back
            </button>
          </div>
        )}

        {/* Playing Screen */}
        {gameState === 'playing' && currentQuestion && (
          <div>
            <div className="text-center mb-6">
              <h2 className="text-xl font-semibold text-gray-800">Question 1</h2>
              <p className="text-sm text-gray-600">Score: {score}</p>
            </div>

            <div className="mb-6">
              <h3 className="text-lg font-medium text-gray-800 mb-4">
                {currentQuestion.question}
              </h3>

              <div className="space-y-2">
                {currentQuestion.options.map((option, index) => (
                  <button
                    key={index}
                    onClick={() => setSelectedAnswer(option)}
                    className={`w-full p-3 text-left rounded-lg border transition-colors ${selectedAnswer === option
                      ? 'bg-blue-100 border-blue-500 text-blue-700'
                      : 'bg-gray-50 border-gray-200 hover:bg-gray-100'
                      }`}
                  >
                    {option}
                  </button>
                ))}
              </div>
            </div>

            <button
              onClick={handleAnswerSubmit}
              disabled={!selectedAnswer}
              className="w-full bg-green-500 hover:bg-green-600 disabled:bg-gray-300 text-white font-semibold py-3 px-6 rounded-lg transition-colors"
            >
              Submit Answer
            </button>
          </div>
        )}

        {/* Finished Screen */}
        {gameState === 'finished' && (
          <div className="text-center">
            <h2 className="text-2xl font-bold text-gray-800 mb-6">Leaderboard</h2>
            <table className="w-full mb-6">
              <thead>
                <tr>
                  <th className="text-left text-gray-600 pb-2">Rank</th>
                  <th className="text-left text-gray-600 pb-2">Name</th>
                  <th className="text-left text-gray-600 pb-2">Score</th>
                </tr>
              </thead>
              <tbody>
                {leaderboard.map((user, idx) => (
                  <tr key={user.name} className={user.name === playerName ? "bg-blue-50 font-bold" : ""}>
                    <td className="py-1">{idx + 1}</td>
                    <td className="py-1">{user.name}</td>
                    <td className="py-1">{user.score}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <button
              onClick={resetGame}
              className="w-full bg-gray-500 hover:bg-gray-600 text-white font-semibold py-3 px-6 rounded-lg transition-colors"
            >
              Back to Main
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

function App() {
  return <TriviaGame />;
}

export default App;