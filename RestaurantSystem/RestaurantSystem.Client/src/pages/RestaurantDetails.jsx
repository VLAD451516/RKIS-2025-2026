import { useState, useEffect } from 'react';
import { Container, Row, Col, Card, Button, Spinner, Alert } from 'react-bootstrap';
import { useParams, useNavigate } from 'react-router-dom';
import { toast } from 'react-toastify';
import api from '../api/axios';
import { useAuth } from '../context/AuthContext';

const RestaurantDetails = () => {
  const { id } = useParams();
  const [restaurant, setRestaurant] = useState(null);
  const [loading, setLoading] = useState(true);
  const [orderItems, setOrderItems] = useState([]);
  const [orderStatus, setOrderStatus] = useState(null);
  const { user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    fetchRestaurant();
  }, [id]);

  const fetchRestaurant = async () => {
    try {
      const response = await api.get(`/restaurants/${id}`);
      setRestaurant(response.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const addToOrder = (item) => {
    setOrderItems(prev => {
      const existing = prev.find(i => i.menuItemId === item.id);
      if (existing) {
        return prev.map(i => i.menuItemId === item.id ? { ...i, quantity: i.quantity + 1 } : i);
      }
      return [...prev, { menuItemId: item.id, name: item.name, price: item.price, quantity: 1 }];
    });
  };

  const handlePlaceOrder = async () => {
    if (!user) {
      navigate('/login');
      return;
    }
    try {
      await api.post('/orders', { items: orderItems.map(({menuItemId, quantity}) => ({menuItemId, quantity})) });
      toast.success('Order placed successfully!');
      setOrderItems([]);
    } catch (err) {
      // toast.error is handled by axios interceptor if it returns a specific error
    }
  };

  if (loading) return <div className="text-center mt-5"><Spinner animation="border" /></div>;
  if (!restaurant) return <Alert variant="danger">Restaurant not found</Alert>;

  return (
    <Container className="py-4">
      <h1>{restaurant.name}</h1>
      <p className="lead">{restaurant.description}</p>
      <p><strong>Address:</strong> {restaurant.address}</p>

      <Row className="mt-4">
        <Col md={8}>
          <h3>Menu</h3>
          <Row>
            {restaurant.menuItems.map(item => (
              <Col key={item.id} md={6} className="mb-3">
                <Card>
                  <Card.Body>
                    <Card.Title>{item.name}</Card.Title>
                    <Card.Text>{item.description}</Card.Text>
                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <span className="h5 mb-0">${item.price}</span>
                      <Button variant="outline-success" onClick={() => addToOrder(item)}>Add to Order</Button>
                    </div>
                    {item.imagePath && (
                      <Button
                        variant="link"
                        size="sm"
                        className="p-0 text-decoration-none"
                        onClick={() => window.open(`http://localhost:5137/${item.imagePath}`, '_blank')}
                      >
                        Download Image
                      </Button>
                    )}
                  </Card.Body>
                </Card>
              </Col>
            ))}
          </Row>
        </Col>
        <Col md={4}>
          <Card>
            <Card.Body>
              <Card.Title>Your Order</Card.Title>
              {orderItems.length === 0 ? <p>Order is empty</p> : (
                <>
                  {orderItems.map(i => (
                    <div key={i.menuItemId} className="d-flex justify-content-between mb-2">
                      <span>{i.name} x {i.quantity}</span>
                      <span>${(i.price * i.quantity).toFixed(2)}</span>
                    </div>
                  ))}
                  <hr />
                  <div className="d-flex justify-content-between h5">
                    <span>Total:</span>
                    <span>${orderItems.reduce((sum, i) => sum + i.price * i.quantity, 0).toFixed(2)}</span>
                  </div>
                  <Button className="w-100 mt-3" onClick={handlePlaceOrder}>Place Order</Button>
                </>
              )}
              {orderStatus && <Alert variant={orderStatus.includes('success') ? 'success' : 'danger'} className="mt-3">{orderStatus}</Alert>}
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default RestaurantDetails;
