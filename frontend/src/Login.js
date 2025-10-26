import React, { useState } from 'react';
import './Login.css';
import LiquidChrome from './LiquidChrome';

const Login = ({ onLoginSuccess }) => {
    const [isRegister, setIsRegister] = useState(false);
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        const endpoint = isRegister ? 'register' : 'login';
        const url = `https://localhost:5001/api/auth/${endpoint}`;

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ username, password }),
            });

            if (response.ok) {
                if (isRegister) {
                    const data = await response.json();
                    setError('Registration successful! Please login.');
                    setIsRegister(false);
                    setPassword('');
                } else {
                    const token = await response.text();
                    localStorage.setItem('token', token);
                    localStorage.setItem('username', username);
                    onLoginSuccess(username);
                }
            } else {
                const errorText = await response.text();
                setError(errorText || 'An error occurred');
            }
        } catch (err) {
            setError('Failed to connect to server');
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    return (
        <>
            <div className="liquid-chrome-background">
                <LiquidChrome
                    baseColor={[0.7, 0.2, 0.9]}
                    speed={0.5}
                    amplitude={0.4}
                    frequencyX={2}
                    frequencyY={2}
                    interactive={true}
                />
            </div>
            <div className="login-container">
                <div className="login-box">
                    <h1>Trivia Game</h1>
                    <h2>{isRegister ? 'Register' : 'Login'}</h2>

                    <form onSubmit={handleSubmit}>
                        <div className="form-group">
                            <label>Username</label>
                            <input
                                type="text"
                                value={username}
                                onChange={(e) => setUsername(e.target.value)}
                                required
                                placeholder="Enter username"
                            />
                        </div>

                        <div className="form-group">
                            <label>Password</label>
                            <input
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                required
                                placeholder="Enter password"
                            />
                        </div>

                        {error && <div className="error-message">{error}</div>}

                        <button type="submit" disabled={loading}>
                            {loading ? 'Please wait...' : isRegister ? 'Register' : 'Login'}
                        </button>
                    </form>

                    <div className="toggle-form">
                        {isRegister ? (
                            <p>
                                Already have an account?{' '}
                                <button type="button" onClick={() => { setIsRegister(false); setError(''); }}>
                                    Login
                                </button>
                            </p>
                        ) : (
                            <p>
                                Don't have an account?{' '}
                                <button type="button" onClick={() => { setIsRegister(true); setError(''); }}>
                                    Register
                                </button>
                            </p>
                        )}
                    </div>
                </div>
            </div>
        </>
    );
};

export default Login;