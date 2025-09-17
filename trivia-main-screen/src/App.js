import React, { useState } from 'react';
import './App.css';

const TriviaGame = () => {
  const [gameState, setGameState] = useState('main');
  const [playerName, setPlayerName] = useState('');
  const [currentQuestion, setCurrentQuestion] = useState(null);
  const [score, setScore] = useState(0);
  const [selectedAnswer, setSelectedAnswer] = useState('');

  // this will later be fetched from an API, now it's hardcoded for simplicity
  const mockQuestion = {
    id: 1,
    question: "What is the capital of France?",
    options: ["London", "Berlin", "Paris", "Madrid"],
    correctAnswer: "Paris"
  };

  const handleStartGame = () => {
    setGameState('nickname');
  };

  const handleJoinGame = () => {
    if (playerName.trim()) {
      setGameState('playing');
      setCurrentQuestion(mockQuestion);
    }
  };

  const handleAnswerSubmit = () => {
    if (selectedAnswer === currentQuestion.correctAnswer) {
      setScore(score + 1);
    }
    setGameState('finished');
  };

  const resetGame = () => {
    setGameState('main');
    setPlayerName('');
    setCurrentQuestion(null);
    setScore(0);
    setSelectedAnswer('');
  };

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full">
        
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
                    className={`w-full p-3 text-left rounded-lg border transition-colors ${
                      selectedAnswer === option
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
            <h2 className="text-2xl font-bold text-gray-800 mb-4">Game Over!</h2>
            <div className="mb-6">
              <p className="text-lg text-gray-700 mb-2">Final Score</p>
              <p className="text-3xl font-bold text-blue-600">{score}</p>
            </div>
            <button
              onClick={resetGame}
              className="w-full bg-blue-500 hover:bg-blue-600 text-white font-semibold py-3 px-6 rounded-lg transition-colors"
            >
              Play Again
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