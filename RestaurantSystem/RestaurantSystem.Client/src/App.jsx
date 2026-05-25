import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import { Navbar, Nav, Container, Button } from 'react-bootstrap';
import { useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import RestaurantList from './pages/RestaurantList';
import RestaurantDetails from './pages/RestaurantDetails';
import Login from './pages/Login';
import Register from './pages/Register';
import { AuthProvider, useAuth } from './context/AuthContext';
import { ToastContainer } from 'react-toastify';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'react-toastify/dist/ReactToastify.css';

const AppContent = () => {
  const { user, logout } = useAuth();

  useEffect(() => {
    if (user && user.token) {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5137/hubs/order", {
          accessTokenFactory: () => user.token
        })
        .withAutomaticReconnect()
        .build();

      connection.start()
        .then(() => {
          console.log("SignalR Connected");
          connection.on("OrderCreated", (order) => {
            console.log("New order created:", order);
            alert(`New order #${order.id} placed for $${order.totalAmount}`);
          });
        })
        .catch(err => console.error("SignalR Connection Error: ", err));

      return () => {
        connection.stop();
      };
    }
  }, [user]);

  return (
    <Router>
      <Navbar bg="dark" variant="dark" expand="lg">
        <Container>
          <Navbar.Brand as={Link} to="/">Restaurant System</Navbar.Brand>
          <Navbar.Toggle aria-controls="basic-navbar-nav" />
          <Navbar.Collapse id="basic-navbar-nav">
            <Nav className="me-auto">
              <Nav.Link as={Link} to="/">Home</Nav.Link>
            </Nav>
            <Nav>
              {user ? (
                <>
                  <Nav.Link disabled>Hello, {user.email}</Nav.Link>
                  <Button variant="outline-light" onClick={logout}>Logout</Button>
                </>
              ) : (
                <Nav.Link as={Link} to="/login">Login</Nav.Link>
              )}
            </Nav>
          </Navbar.Collapse>
        </Container>
      </Navbar>

      <Routes>
        <Route path="/" element={<RestaurantList />} />
        <Route path="/restaurant/:id" element={<RestaurantDetails />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
      </Routes>
      <ToastContainer position="bottom-right" />
    </Router>
  );
};

function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}

export default App;
